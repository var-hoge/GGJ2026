using System;
using System.Collections.Generic;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// キャラクター選択画面の遷移とキー操作を管理する。
/// 誰がどのキャラクターを選んでいるかを、ボタンの上の「あなた」「相手」の目印で示す。
/// </summary>
public class CharacterSelectionScreenManager : MonoBehaviour
{
    /// <summary>キャラクター1体分のボタンと、誰が選んでいるかを示す目印。</summary>
    [Serializable]
    class CharacterEntry
    {
        [Tooltip("このキャラクターを選ぶボタン")]
        public GameObject Button;
        [Tooltip("自分が選んでいるときだけ表示する目印 (ボタンの子の You)")]
        public GameObject YouMarker;
        [Tooltip("相手が選んでいるときだけ表示する目印 (ボタンの子の Opponent)")]
        public GameObject OpponentMarker;

        RectTransform _youRect;
        RectTransform _opponentRect;
        float _youPairedPosX;
        float _opponentPairedPosX;

        /// <summary>シーンで置いてある位置を、2つ並ぶときの位置として覚えておく。</summary>
        public void Initialize()
        {
            _youRect = (RectTransform)YouMarker.transform;
            _opponentRect = (RectTransform)OpponentMarker.transform;
            _youPairedPosX = _youRect.anchoredPosition.x;
            _opponentPairedPosX = _opponentRect.anchoredPosition.x;
        }

        public void SetMarkers(bool you, bool opponent, float centeredPosX)
        {
            YouMarker.SetActive(you);
            OpponentMarker.SetActive(opponent);

            // このボタンに目印が1つしか出ないなら中央に寄せ、2つ並ぶならシーンで置いた位置に戻す
            var alone = you != opponent;
            SetPosX(_youRect, alone ? centeredPosX : _youPairedPosX);
            SetPosX(_opponentRect, alone ? centeredPosX : _opponentPairedPosX);
        }

        static void SetPosX(RectTransform rect, float posX)
        {
            var position = rect.anchoredPosition;
            position.x = posX;
            rect.anchoredPosition = position;
        }
    }

    [SerializeField] GameObject _defaultSelectedButton;

    [Header("キャラクター")]
    [SerializeField] CharacterEntry _phantomCat;
    [SerializeField] CharacterEntry _policeDog;

    [Tooltip("目印が1つだけのときの PosX。子の Text の中央がボタンの中央に来る値")]
    [SerializeField] float _centeredMarkerPosX = -52.8875f;

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

    /// <summary>UI の移動操作。デバッグ中だけ A / D の割り当てを外すために持っておく。</summary>
    InputAction _moveAction;

    readonly List<int> _debugKeyBindings = new List<int>();

    void Start()
    {
        _phantomCat.Initialize();
        _policeDog.Initialize();

        _youCharacter = CharacterOf(_defaultSelectedButton) ?? PlayableCharacter.PhantomCat;
        ApplyMarkers();

        FindDebugKeyBindings();
    }

    void OnDisable()
    {
        // 移動操作の割り当ては画面をまたいで残るので、必ず元に戻してから抜ける
        SetDebugKeysUsedForNavigation(true);
        _wasDebugMode = false;
    }

    void Update()
    {
        UpdateDebugOpponent();

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
        ApplyMarkers();
    }

    void SetYouCharacter(PlayableCharacter character)
    {
        if (_youCharacter == character)
        {
            return;
        }

        _youCharacter = character;
        ApplyMarkers();
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

    void ApplyMarkers()
    {
        _phantomCat.SetMarkers(
            _youCharacter == PlayableCharacter.PhantomCat,
            _opponentCharacter == PlayableCharacter.PhantomCat,
            _centeredMarkerPosX);
        _policeDog.SetMarkers(
            _youCharacter == PlayableCharacter.PoliceDog,
            _opponentCharacter == PlayableCharacter.PoliceDog,
            _centeredMarkerPosX);
    }

    /// <summary>そのゲームオブジェクトがキャラクターのボタンなら、対応するキャラクターを返す。</summary>
    PlayableCharacter? CharacterOf(GameObject buttonObject)
    {
        if (buttonObject == _phantomCat.Button) return PlayableCharacter.PhantomCat;
        if (buttonObject == _policeDog.Button) return PlayableCharacter.PoliceDog;
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
        CharacterSelection.Select(character);
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
