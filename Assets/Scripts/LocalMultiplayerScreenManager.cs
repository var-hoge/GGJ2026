using System.Collections.Generic;
using KanKikuchi.AudioManager;
using PhantomCatWorks.RealtimeP2PKit;
using PhantomCatWorks.RealtimeP2PKit.Lan;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ローカル対戦画面の遷移とキー操作を管理する。
/// 同一LAN上のロビーをUDPブロードキャストで探し、見つかった数に応じてロビーボタンの表示を
/// 切り替え、その並びに合わせてキー操作の移動先を繋ぎ直す。
///
/// ロビーボタンの onClick はシーンの設定ではなく、この場で「N番目のロビーに参加する」へ
/// 張り替える (見つかったロビーは実行時にしか決まらないため)。
/// 一方、ロビーを新規作成するボタンはシーンから <see cref="OnEnterLobby"/> を呼ぶ従来のままでよい。
/// </summary>
public class LocalMultiplayerScreenManager : MonoBehaviour
{
    [SerializeField] GameObject _defaultSelectedButton;

    [Header("ロビー")]
    [Tooltip("画面の上から並んでいる順に設定する")]
    [SerializeField] GameObject[] _lobbyButtons;
    [Tooltip("ロビーが1つも見つかっていないときだけ表示する")]
    [SerializeField] GameObject _waitingText;
    [Tooltip("自分がロビーを作って相手を待っている間だけ表示する。未設定でも動く")]
    [SerializeField] GameObject _hostingText;

    [Header("キー操作の移動順")]
    [Tooltip("画面の上から並んでいる順に設定する。非表示のものは飛ばして上下が繋がる")]
    [SerializeField] Selectable[] _verticalNavigationOrder;

    [Header("決定したときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] float _submitRumbleStrength = 0.3f;
    [SerializeField] float _submitRumbleDuration = 0.1f;

    readonly List<Selectable> _navigableBuffer = new List<Selectable>();

    /// <summary>並びの先頭の上方向だけはシーンで設定した移動先 (戻るボタン) を使い続ける。</summary>
    Selectable _firstUpNeighbour;

    int _appliedLobbyCount = -1;

    /// <summary>ロビーを作って相手を待っている間は、他の操作を受け付けない。</summary>
    bool _isHosting;

    /// <summary>参加処理中の多重実行を防ぐ。</summary>
    bool _isJoining;

    void Awake()
    {
        if (_verticalNavigationOrder.Length > 0 && _verticalNavigationOrder[0] != null)
        {
            _firstUpNeighbour = _verticalNavigationOrder[0].navigation.selectOnUp;
        }

        BindLobbyButtons();
    }

    void OnEnable()
    {
        var session = NetSession.Instance;
        session.LobbiesChanged += OnLobbiesChanged;
        session.Connected += OnConnected;
        session.Disconnected += OnDisconnected;
        session.BeginLanDiscovery();

        SetFoundLobbyCount(0);
        if (_hostingText != null) _hostingText.SetActive(false);
    }

    void OnDisable()
    {
        // シーンを抜けるときも接続自体は次の画面へ引き継ぐので、探索だけを止める
        var session = NetSession.Instance;
        session.LobbiesChanged -= OnLobbiesChanged;
        session.Connected -= OnConnected;
        session.Disconnected -= OnDisconnected;
        session.EndLanDiscovery();
    }

    void Update()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        // 背景クリック等で選択が外れてもパッド操作が効くように復帰させる
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            EventSystem.current.SetSelectedGameObject(_defaultSelectedButton);
            return;
        }

        // スペースキーでも決定できるようにする (標準の Submit は Enter のみ)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }

    /// <summary>
    /// ロビーボタンの onClick を「N番目のロビーに参加する」へ差し替える。
    /// シーン側に元から設定されている呼び出しは、二重に走らないよう切っておく。
    /// </summary>
    void BindLobbyButtons()
    {
        for (var i = 0; i < _lobbyButtons.Length; i++)
        {
            if (_lobbyButtons[i] == null) continue;

            var button = _lobbyButtons[i].GetComponent<Button>();
            if (button == null) continue;

            for (var p = 0; p < button.onClick.GetPersistentEventCount(); p++)
            {
                button.onClick.SetPersistentListenerState(p, UnityEventCallState.Off);
            }

            button.onClick.RemoveAllListeners();

            var index = i; // クロージャがループ変数を共有しないように退避する
            button.onClick.AddListener(() => OnJoinLobby(index));
        }
    }

    void OnLobbiesChanged()
    {
        if (_isHosting) return;

        var lobbies = NetSession.Instance.Lobbies;
        SetFoundLobbyCount(lobbies.Count);
        ApplyLobbyLabels(lobbies);
    }

    /// <summary>ボタンの文字を、実際に見つかったロビーの名前にする。</summary>
    void ApplyLobbyLabels(IReadOnlyList<LanLobbyInfo> lobbies)
    {
        for (var i = 0; i < _lobbyButtons.Length && i < lobbies.Count; i++)
        {
            if (_lobbyButtons[i] == null) continue;

            // 多言語化された固定文言が乗っているボタンは、そちらに任せて触らない
            if (_lobbyButtons[i].GetComponentInChildren<LocalizedText>() != null) continue;

            var label = _lobbyButtons[i].GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) label.text = lobbies[i].LobbyName;
        }
    }

    /// <summary>見つかったロビーの数だけロビーボタンを表示する。</summary>
    public void SetFoundLobbyCount(int count)
    {
        count = Mathf.Clamp(count, 0, _lobbyButtons.Length);
        if (count == _appliedLobbyCount)
        {
            return;
        }

        _appliedLobbyCount = count;

        for (var i = 0; i < _lobbyButtons.Length; i++)
        {
            if (_lobbyButtons[i] != null) _lobbyButtons[i].SetActive(i < count);
        }

        if (_waitingText != null) _waitingText.SetActive(count == 0);

        RebuildNavigation();
    }

    /// <summary>表示中のものだけを上下に繋ぎ直し、非表示のロビーボタンで移動が止まらないようにする。</summary>
    void RebuildNavigation()
    {
        _navigableBuffer.Clear();
        foreach (var selectable in _verticalNavigationOrder)
        {
            if (selectable != null && selectable.gameObject.activeInHierarchy)
            {
                _navigableBuffer.Add(selectable);
            }
        }

        for (var i = 0; i < _navigableBuffer.Count; i++)
        {
            var navigation = _navigableBuffer[i].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = i > 0 ? _navigableBuffer[i - 1] : _firstUpNeighbour;
            navigation.selectOnDown = i < _navigableBuffer.Count - 1 ? _navigableBuffer[i + 1] : null;
            _navigableBuffer[i].navigation = navigation;
        }
    }

    /// <summary>
    /// ロビーを新規作成して相手を待つ。シーンの「ロビーを作る」ボタンから呼ぶ。
    /// 相手が入ってきたら <see cref="OnConnected"/> でキャラクター選択へ進む。
    /// </summary>
    public void OnEnterLobby()
    {
        if (_isHosting || _isJoining) return;

        var playerName = SystemInfo.deviceName;
        if (!NetSession.Instance.StartLanHost($"{playerName} のロビー", playerName))
        {
            Debug.LogError("ロビーを作成できませんでした。ポートが使用中か、ネットワークが利用できません");
            return;
        }

        _isHosting = true;

        // 待っている間は自分のロビーしか無いので、一覧は畳んで待機表示に切り替える
        SetFoundLobbyCount(0);
        if (_waitingText != null) _waitingText.SetActive(false);
        if (_hostingText != null) _hostingText.SetActive(true);

        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);
    }

    /// <summary>一覧のN番目のロビーに参加する。ボタンの onClick から実行時に呼ばれる。</summary>
    public async void OnJoinLobby(int index)
    {
        if (_isHosting || _isJoining) return;

        var lobbies = NetSession.Instance.Lobbies;
        if (index < 0 || index >= lobbies.Count) return;

        _isJoining = true;
        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);

        var joined = await NetSession.Instance.JoinLanLobbyAsync(lobbies[index]);
        if (!joined)
        {
            Debug.LogWarning("ロビーへの参加に失敗しました");
            _isJoining = false;
        }

        // 成功した場合は OnConnected 側で次の画面へ進む
    }

    /// <summary>ホスト・参加のどちらでも、繋がったらキャラクター選択へ進む。</summary>
    void OnConnected()
    {
        LoadScene("CharacterSelectionScreen");
    }

    void OnDisconnected(string reason)
    {
        Debug.LogWarning($"接続できませんでした: {reason}");
        _isHosting = false;
        _isJoining = false;

        NetSession.Instance.Shutdown();
        if (_hostingText != null) _hostingText.SetActive(false);

        // 探索からやり直せるようにする
        NetSession.Instance.BeginLanDiscovery();
        SetFoundLobbyCount(NetSession.Instance.Lobbies.Count);
    }

    public void OnBack()
    {
        // 待機中のロビーを残したままにしないよう、通信を完全に畳んでから戻る
        NetSession.Instance.Shutdown();
        NetGameState.Clear();
        LoadScene("GameSettingsScreen");
    }

    void LoadScene(string sceneName)
    {
        ScreenHistory.LoadScene(sceneName);
        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);
    }
}
