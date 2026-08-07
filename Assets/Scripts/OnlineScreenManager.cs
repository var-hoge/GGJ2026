using System.Collections.Generic;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// オンライン対戦画面の遷移とキー操作を管理する。
/// 見つかったロビーの数に応じてロビーボタンの表示を切り替え、その並びに合わせてキー操作の移動先を繋ぎ直す。
/// </summary>
public class OnlineScreenManager : MonoBehaviour
{
    const int MaxLobbyCount = 3;

    [SerializeField] GameObject _defaultSelectedButton;

    [Header("ロビー")]
    [Tooltip("画面の上から並んでいる順に設定する")]
    [SerializeField] GameObject[] _lobbyButtons;
    [Tooltip("ロビーが1つも見つかっていないときだけ表示する")]
    [SerializeField] GameObject _waitingText;

    [Header("キー操作の移動順")]
    [Tooltip("画面の上から並んでいる順に設定する。非表示のものは飛ばして上下が繋がる")]
    [SerializeField] Selectable[] _verticalNavigationOrder;

    [Header("デバッグ (ロビー検索が未実装のための暫定機能)")]
    [Tooltip("見つかったロビーの数。再生中に変えると表示が切り替わる")]
    [SerializeField, Range(0, MaxLobbyCount)] int _debugFoundLobbyCount;

    [Header("決定したときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] float _submitRumbleStrength = 0.3f;
    [SerializeField] float _submitRumbleDuration = 0.1f;

    readonly List<Selectable> _navigableBuffer = new List<Selectable>();

    /// <summary>並びの先頭の上方向だけはシーンで設定した移動先 (戻るボタン) を使い続ける。</summary>
    Selectable _firstUpNeighbour;

    int _appliedLobbyCount = -1;

    void Awake()
    {
        if (_verticalNavigationOrder.Length > 0 && _verticalNavigationOrder[0] != null)
        {
            _firstUpNeighbour = _verticalNavigationOrder[0].navigation.selectOnUp;
        }
    }

    void Start()
    {
        //SetFoundLobbyCount(_debugFoundLobbyCount);
    }

    void Update()
    {
        // インスペクターでデバッグ用の数を変えたら再生中でも反映する
        //SetFoundLobbyCount(_debugFoundLobbyCount);

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
    /// 見つかったロビーの数だけロビーボタンを表示する。ロビー検索の実装後はそちらから呼ぶ。
    /// </summary>
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
            _lobbyButtons[i].SetActive(i < count);
        }

        _waitingText.SetActive(count == 0);

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

    public void OnBack()
    {
        LoadScene("GameSettingsScreen");
    }

    void LoadScene(string sceneName)
    {
        ScreenHistory.LoadScene(sceneName);
        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);
    }
}
