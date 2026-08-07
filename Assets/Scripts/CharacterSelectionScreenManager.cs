using System;
using System.Collections.Generic;
using DG.Tweening;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using MessagePack;
using PhantomCatWorks.RealtimeP2PKit;

/// <summary>
/// キャラクター選択画面の遷移とキー操作を管理する。
/// 誰がどのキャラクターを選んでいるかは、ボタンの上を移動する「あなた」「相手」のカーソルで示す。
/// </summary>
public class CharacterSelectionScreenManager : MonoBehaviour
{
    // packetId=1 is the P2P demo position packet. Keep game UI packets separate.
    private const byte CharacterSelectionPacketId = 2;
    private const float SelectionResendIntervalSeconds = 0.5f;

    [MessagePackObject(AllowPrivate = true)]
    internal struct CharacterSelectionPacket
    {
        [Key(0)] public int Character;
        [Key(1)] public bool Confirmed;
    }
    [SerializeField] GameObject _defaultSelectedButton;

    [Header("キャラクターのボタン")]
    [SerializeField] RectTransform _phantomCatButton;
    [SerializeField] RectTransform _policeDogButton;

    [Header("選んでいるキャラクターを示すカーソル")]
    [Tooltip("ボタンと同じ親 (Canvas) に置く。ボタンの子にするとボタンの拡大に巻き込まれる")]
    [SerializeField] RectTransform _youCursor;
    [SerializeField] RectTransform _opponentCursor;
    [Tooltip("ボタンの上端からの高さ")]
    [SerializeField] float _cursorOffsetY = 5f;
    [Tooltip("1つだけのときの、ボタン中央からの左右のずれ。子の Text の中央がボタンの中央に来る値")]
    [SerializeField] float _aloneOffsetX = -52.8875f;
    [Tooltip("2つ並ぶときの、ボタン中央からの左右のずれ")]
    [SerializeField] float _pairedYouOffsetX = -80f;
    [SerializeField] float _pairedOpponentOffsetX = 30f;
    [Tooltip("カーソルが動く時間")]
    [SerializeField] float _cursorMoveDuration = 0.2f;

    [Header("戻る先")]
    [Tooltip("直前の画面が分からないとき (このシーンを直接再生したときなど) に戻る画面")]
    [SerializeField] string _fallbackBackScene = "LocalMultiplayerScreen";

    [Header("決定したときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] float _submitRumbleStrength = 0.3f;
    [SerializeField] float _submitRumbleDuration = 0.1f;

    [Header("デバッグ (通信対戦が未実装のための暫定機能)")]
    [Tooltip("オンにすると A / D キーで相手が選んでいるキャラクターを動かせる。その間 A / D では自分の選択は動かない")]
    [SerializeField] bool _debugMode;

    /// <summary>相手を動かすデバッグキー。UI の左右移動にも割り当てられている。</summary>
    static readonly string[] DebugKeyPaths = { "<Keyboard>/a", "<Keyboard>/d" };

    /// <summary>自分が選んでいるキャラクター。戻るボタンへ移動しても直前の選択を保つ。</summary>
    PlayableCharacter _youCharacter;

    /// <summary>相手が選んでいるキャラクター。まだ選んでいなければ null。</summary>
    PlayableCharacter? _opponentCharacter;

    bool _wasDebugMode;
    bool _networkSyncEnabled;
    float _nextSelectionSyncTime;
    bool _localCharacterConfirmed;
    bool _opponentCharacterConfirmed;
    bool _transitioningToStory;

    /// <summary>UI の移動操作。デバッグ中だけ A / D の割り当てを外すために持っておく。</summary>
    InputAction _moveAction;

    readonly List<int> _debugKeyBindings = new List<int>();

    void Start()
    {
        _youCharacter = CharacterOf(_defaultSelectedButton) ?? PlayableCharacter.PhantomCat;

        // 画面が出た時点ではカーソルは動かさず、選ばれているところに置く
        ApplyCursors(instant: true);

        FindDebugKeyBindings();
        StartOnlineSelectionSync();
    }

    void OnDisable()
    {
        // 移動操作の割り当ては画面をまたいで残るので、必ず元に戻してから抜ける
        SetDebugKeysUsedForNavigation(true);
        _wasDebugMode = false;
    }

    void OnDestroy()
    {
        if (_networkSyncEnabled)
            P2PManager.Instance.UnregisterPacketHandler(CharacterSelectionPacketId);
        _youCursor.DOKill();
        _opponentCursor.DOKill();
    }

    void Update()
    {
        UpdateDebugOpponent();

        // The channel is usually configured as unreliable for gameplay. Re-send
        // the current selection so a lost packet is automatically corrected.
        if (_networkSyncEnabled && Time.unscaledTime >= _nextSelectionSyncTime)
        {
            SendLocalCharacter();
            _nextSelectionSyncTime = Time.unscaledTime + SelectionResendIntervalSeconds;
        }

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

        var character = CharacterOf(selected);
        if (character.HasValue)
        {
            SetYouCharacter(character.Value);
        }

        // スペースキーでも決定できるようにする (標準の Submit は Enter のみ)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }

    /// <summary>通信対戦の実装後は、相手から届いた選択でこれを呼ぶ。</summary>
    public void SetOpponentCharacter(PlayableCharacter? character)
    {
        if (_opponentCharacter == character)
        {
            return;
        }

        _opponentCharacter = character;
        ApplyCursors(instant: false);
    }

    void SetYouCharacter(PlayableCharacter character)
    {
        if (_youCharacter == character)
        {
            return;
        }

        _youCharacter = character;
        ApplyCursors(instant: false);
        SendLocalCharacter();
    }

    private void StartOnlineSelectionSync()
    {
        // Local multiplayer and standalone character selection must remain
        // usable without creating a P2P session.
        if (!P2PManager.Instance.IsOnlineMatch ||
            P2PManager.Instance.Session.State != P2PSessionState.Connected)
        {
            Debug.Log("[CharacterSelection] offline/local mode; network selection sync is disabled");
            return;
        }

        _networkSyncEnabled = true;
        P2PManager.Instance.RegisterPacketHandler<CharacterSelectionPacket>(
            CharacterSelectionPacketId, OnOpponentCharacterPacket);
        SendLocalCharacter();
        _nextSelectionSyncTime = Time.unscaledTime + SelectionResendIntervalSeconds;
        Debug.Log("[CharacterSelection] P2P session retained; opponent selection sync enabled");
    }

    private void SendLocalCharacter()
    {
        if (!_networkSyncEnabled) return;
        P2PManager.Instance.Send(CharacterSelectionPacketId, new CharacterSelectionPacket
        {
            Character = (int)_youCharacter,
            Confirmed = _localCharacterConfirmed,
        });
    }

    private void OnOpponentCharacterPacket(CharacterSelectionPacket packet)
    {
        if (packet.Character < (int)PlayableCharacter.PhantomCat ||
            packet.Character > (int)PlayableCharacter.PoliceDog)
        {
            Debug.LogWarning($"[CharacterSelection] ignored invalid opponent character: {packet.Character}");
            return;
        }

        SetOpponentCharacter((PlayableCharacter)packet.Character);
        if (packet.Confirmed)
            HandleOpponentCharacterConfirmed((PlayableCharacter)packet.Character);
        if (P2PLog.ShouldLog(P2PLogLevel.Info))
            Debug.Log($"[CharacterSelection] opponent selected {(PlayableCharacter)packet.Character} confirmed={packet.Confirmed}");
    }

    private void HandleOpponentCharacterConfirmed(PlayableCharacter opponentCharacter)
    {
        _opponentCharacterConfirmed = true;

        if (!_localCharacterConfirmed)
        {
            // The player who receives the first confirmation is assigned the
            // other role and immediately acknowledges it to the chooser.
            ConfirmLocalCharacter(OtherCharacter(opponentCharacter), wasAutoSelected: true);
            return;
        }

        // If both peers confirm at nearly the same time, the room creator is
        // authoritative. This makes the outcome deterministic and still keeps
        // the two roles different.
        if (!P2PManager.Instance.Session.IsInitiator && _youCharacter == opponentCharacter)
        {
            _youCharacter = OtherCharacter(opponentCharacter);
            CharacterSelection.Select(_youCharacter);
            ApplyCursors(instant: false);
            SendLocalCharacter();
        }

        TryEnterIntroStory();
    }

    private static PlayableCharacter OtherCharacter(PlayableCharacter character) =>
        character == PlayableCharacter.PhantomCat ? PlayableCharacter.PoliceDog : PlayableCharacter.PhantomCat;

    /// <summary>
    /// カーソルを、それぞれが選んでいるキャラクターのボタンへ動かす。
    /// 同じキャラクターに2つ並ぶときは左右に振り分け、1つだけなら中央に置く。
    /// </summary>
    void ApplyCursors(bool instant)
    {
        var paired = _opponentCharacter == _youCharacter;

        MoveCursor(_youCursor, _youCharacter, paired ? _pairedYouOffsetX : _aloneOffsetX, instant);

        // 相手がまだ選んでいないうちはカーソルごと出さない。出すときは動かさずその場に置く
        var wasVisible = _opponentCursor.gameObject.activeSelf;
        _opponentCursor.gameObject.SetActive(_opponentCharacter.HasValue);
        if (_opponentCharacter.HasValue)
        {
            MoveCursor(
                _opponentCursor,
                _opponentCharacter.Value,
                paired ? _pairedOpponentOffsetX : _aloneOffsetX,
                instant || !wasVisible);
        }
    }

    void MoveCursor(RectTransform cursor, PlayableCharacter character, float offsetX, bool instant)
    {
        var button = character == PlayableCharacter.PhantomCat ? _phantomCatButton : _policeDogButton;
        var position = new Vector2(
            button.anchoredPosition.x + offsetX,
            button.anchoredPosition.y + button.rect.height * 0.5f + _cursorOffsetY);

        cursor.DOKill();
        if (instant)
        {
            cursor.anchoredPosition = position;
        }
        else
        {
            cursor.DOAnchorPos(position, _cursorMoveDuration).SetEase(Ease.OutBack);
        }
    }

    /// <summary>通信対戦が未実装なので、デバッグモードのときだけキー操作で相手の選択を再現する。</summary>
    void UpdateDebugOpponent()
    {
        // インスペクターで切り替えたら再生中でも反映する
        if (_debugMode != _wasDebugMode)
        {
            _wasDebugMode = _debugMode;
            SetDebugKeysUsedForNavigation(!_debugMode);

            // オフに戻したら、デバッグで選ばせた相手の選択も消す
            if (!_debugMode)
            {
                SetOpponentCharacter(null);
            }
        }

        if (!_debugMode || Keyboard.current == null)
        {
            return;
        }

        // ボタンの並びに合わせて、左のキャラクターが A、右のキャラクターが D
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            SetOpponentCharacter(PlayableCharacter.PhantomCat);
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            SetOpponentCharacter(PlayableCharacter.PoliceDog);
        }
    }

    void FindDebugKeyBindings()
    {
        // EventSystem.currentInputModule は最初の Update まで決まらないので、コンポーネントから直接取る
        var inputModule = EventSystem.current != null
            ? EventSystem.current.GetComponent<InputSystemUIInputModule>()
            : null;
        _moveAction = inputModule != null && inputModule.move != null ? inputModule.move.action : null;
        if (_moveAction == null)
        {
            return;
        }

        var bindings = _moveAction.bindings;
        for (var i = 0; i < bindings.Count; i++)
        {
            if (Array.IndexOf(DebugKeyPaths, bindings[i].path) >= 0)
            {
                _debugKeyBindings.Add(i);
            }
        }
    }

    /// <summary>
    /// A / D キーは UI の左右移動にも割り当てられている。
    /// デバッグ中はその割り当てを外し、相手だけが動いて自分の選択は動かないようにする。
    /// </summary>
    void SetDebugKeysUsedForNavigation(bool used)
    {
        if (_debugKeyBindings.Count == 0)
        {
            // 見つからないまま黙って効かなくなると原因が分かりにくいので知らせる
            if (!used)
            {
                Debug.LogWarning("UI の移動操作に A / D が見つからないので、デバッグ中も自分の選択が動きます", this);
            }

            return;
        }

        foreach (var index in _debugKeyBindings)
        {
            if (used)
            {
                _moveAction.RemoveBindingOverride(index);
            }
            else
            {
                // 空のパスで上書きすると、その割り当てだけを無効にできる
                _moveAction.ApplyBindingOverride(index, string.Empty);
            }
        }
    }

    /// <summary>そのゲームオブジェクトがキャラクターのボタンなら、対応するキャラクターを返す。</summary>
    PlayableCharacter? CharacterOf(GameObject buttonObject)
    {
        if (buttonObject == _phantomCatButton.gameObject) return PlayableCharacter.PhantomCat;
        if (buttonObject == _policeDogButton.gameObject) return PlayableCharacter.PoliceDog;
        return null;
    }

    public void OnSelectPhantomCat()
    {
        SelectCharacter(PlayableCharacter.PhantomCat);
    }

    public void OnSelectPoliceDog()
    {
        SelectCharacter(PlayableCharacter.PoliceDog);
    }

    /// <summary>選んだキャラクターは後続の画面から参照されるので記録してから次へ進む。</summary>
    void SelectCharacter(PlayableCharacter character)
    {
        if (_localCharacterConfirmed) return;

        // Keep local multiplayer behaviour unchanged. Online play waits for
        // the peer to receive the choice and confirm the opposite role.
        if (!_networkSyncEnabled)
        {
            CharacterSelection.Select(character);
            LoadScene("IntroStory");
            return;
        }

        ConfirmLocalCharacter(character, wasAutoSelected: false);
    }

    private void ConfirmLocalCharacter(PlayableCharacter character, bool wasAutoSelected)
    {
        _youCharacter = character;
        _localCharacterConfirmed = true;
        CharacterSelection.Select(character);
        ApplyCursors(instant: false);
        SendLocalCharacter();

        Debug.Log(wasAutoSelected
            ? $"[CharacterSelection] automatically assigned {character} for opponent's choice"
            : $"[CharacterSelection] selected {character}; waiting for opponent confirmation");

        TryEnterIntroStory();
    }

    private void TryEnterIntroStory()
    {
        if (_transitioningToStory || !_localCharacterConfirmed || !_opponentCharacterConfirmed)
            return;

        _transitioningToStory = true;
        Debug.Log($"[CharacterSelection] roles confirmed: local={_youCharacter}, opponent={_opponentCharacter}; loading IntroStory");
        LoadScene("IntroStory");
    }

    /// <summary>
    /// この画面はローカル対戦とオンライン対戦の両方から来るので、遷移元の画面に戻る。
    /// </summary>
    public void OnBack()
    {
        var previous = ScreenHistory.PreviousSceneName;
        LoadScene(string.IsNullOrEmpty(previous) ? _fallbackBackScene : previous);
    }

    void LoadScene(string sceneName)
    {
        ScreenHistory.LoadScene(sceneName);
        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);
    }
}
