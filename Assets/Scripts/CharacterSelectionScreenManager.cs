using DG.Tweening;
using KanKikuchi.AudioManager;
using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// キャラクター選択画面の遷移とキー操作を管理する。
/// 誰がどのキャラクターを選んでいるかは、ボタンの上を移動する「あなた」「相手」のカーソルで示す。
///
/// 通信対戦中は、カーソルを動かした時点で相手へ選択が飛び、決定すると
/// 「両者が決定し、かつ別のキャラクターである」まで待ってから次の画面へ進む。
/// 同じキャラクターを選んだ場合はホスト側が優先され、参加側の決定は取り消される。
/// ソロプレイ (未接続) のときは従来どおり、決定した時点で即座に次へ進む。
/// </summary>
public class CharacterSelectionScreenManager : MonoBehaviour
{
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

    [Header("相手を待っている間の表示")]
    [Tooltip("自分だけ決定して相手待ちのときに出す。未設定でも動く")]
    [SerializeField] GameObject _waitingForOpponentText;

    [Header("戻る先")]
    [Tooltip("直前の画面が分からないとき (このシーンを直接再生したときなど) に戻る画面")]
    [SerializeField] string _fallbackBackScene = "LocalMultiplayerScreen";

    [Header("決定したときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] float _submitRumbleStrength = 0.3f;
    [SerializeField] float _submitRumbleDuration = 0.1f;

    /// <summary>自分が選んでいるキャラクター。戻るボタンへ移動しても直前の選択を保つ。</summary>
    PlayableCharacter _youCharacter;

    /// <summary>相手が選んでいるキャラクター。まだ選んでいなければ null。</summary>
    PlayableCharacter? _opponentCharacter;

    bool _youConfirmed;
    bool _opponentConfirmed;

    /// <summary>ホストがシードを配り終えた (または自分がホストで配った) か。</summary>
    bool _startAgreed;

    bool IsNetworked => NetSession.IsActive;

    void Start()
    {
        _youCharacter = CharacterOf(_defaultSelectedButton) ?? PlayableCharacter.PhantomCat;

        // 画面が出た時点ではカーソルは動かさず、選ばれているところに置く
        ApplyCursors(instant: true);

        if (_waitingForOpponentText != null) _waitingForOpponentText.SetActive(false);

        // 相手にも自分の初期位置を知らせておく
        SendMySelection();
    }

    void OnEnable()
    {
        if (!IsNetworked) return;

        var session = NetSession.Instance;
        session.RegisterPacketHandler<CharacterSelectionPacket>(GameNetPacketId.CharacterSelection, OnOpponentSelection);
        session.RegisterPacketHandler<GameStartPacket>(GameNetPacketId.GameStart, OnGameStart);
        session.Disconnected += OnDisconnected;
    }

    void OnDisable()
    {
        // ソロプレイでは OnEnable で何も登録していない。ここで Instance を触ると
        // 不要な常駐オブジェクトが生まれてしまうので、存在確認してから片付ける
        if (!NetSession.Exists) return;

        var session = NetSession.Instance;
        session.UnregisterPacketHandler(GameNetPacketId.CharacterSelection);
        session.UnregisterPacketHandler(GameNetPacketId.GameStart);
        session.Disconnected -= OnDisconnected;
    }

    void OnDestroy()
    {
        _youCursor.DOKill();
        _opponentCursor.DOKill();
    }

    void Update()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        // 自分が決定した後はカーソルを動かさない (相手待ちのため)
        if (_youConfirmed) return;

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

    // -----------------------------------------------------------------
    // 通信
    // -----------------------------------------------------------------

    void SendMySelection()
    {
        if (!IsNetworked) return;

        NetSession.Instance.Send(GameNetPacketId.CharacterSelection, new CharacterSelectionPacket
        {
            Character = (byte)_youCharacter,
            Confirmed = _youConfirmed,
        });
    }

    void OnOpponentSelection(CharacterSelectionPacket packet)
    {
        var character = packet.AsCharacter();
        _opponentConfirmed = packet.Confirmed;
        SetOpponentCharacter(character);

        // 同じキャラクターを取り合った場合はホストを優先する。
        // 参加側は決定を取り消して選び直す
        if (_youConfirmed && _opponentConfirmed && character == _youCharacter && !NetSession.IsHost)
        {
            _youConfirmed = false;
            if (_waitingForOpponentText != null) _waitingForOpponentText.SetActive(false);
            SEManager.Instance.Play(SEPath.UI_SELECT);
            SendMySelection();
            return;
        }

        TryStartGame();
    }

    void OnGameStart(GameStartPacket packet)
    {
        // 参加側はホストが決めたシードを使う。これが揃わないと猫の配置が食い違う
        NetGameState.SetCatSeed(packet.CatSeed);
        NetGameState.SetOpponentCharacter(_opponentCharacter);
        ProceedToGame();
    }

    /// <summary>両者が別々のキャラクターで決定していれば、ホストが開始を宣言する。</summary>
    void TryStartGame()
    {
        if (_startAgreed) return;
        if (!_youConfirmed || !_opponentConfirmed) return;
        if (_opponentCharacter == _youCharacter) return;

        if (!NetSession.IsHost)
        {
            // 参加側はホストからの GameStart を待つ
            return;
        }

        var seed = Random.Range(int.MinValue, int.MaxValue);
        NetGameState.SetCatSeed(seed);
        NetGameState.SetOpponentCharacter(_opponentCharacter);
        NetSession.Instance.Send(GameNetPacketId.GameStart, new GameStartPacket { CatSeed = seed });
        ProceedToGame();
    }

    void ProceedToGame()
    {
        if (_startAgreed) return;
        _startAgreed = true;

        CharacterSelection.Select(_youCharacter);
        LoadScene("IntroStory");
    }

    void OnDisconnected(string reason)
    {
        Debug.LogWarning($"対戦相手との接続が切れました: {reason}");
        NetSession.Instance.Shutdown();
        NetGameState.Clear();
        LoadScene(_fallbackBackScene);
    }

    // -----------------------------------------------------------------
    // カーソル表示
    // -----------------------------------------------------------------

    /// <summary>相手から届いた選択を反映する。</summary>
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

        // カーソルを動かすたびに相手へ知らせるので、相手の画面でもリアルタイムに動く
        SendMySelection();
    }

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
        if (!IsNetworked)
        {
            // ソロプレイは従来どおり即座に進む
            CharacterSelection.Select(character);
            LoadScene("IntroStory");
            return;
        }

        if (_youConfirmed) return;

        // 相手が既に決定しているキャラクターは選べない
        if (_opponentConfirmed && _opponentCharacter == character)
        {
            SEManager.Instance.Play(SEPath.UI_SELECT);
            return;
        }

        _youCharacter = character;
        _youConfirmed = true;
        if (_waitingForOpponentText != null) _waitingForOpponentText.SetActive(true);

        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);

        SendMySelection();
        TryStartGame();
    }

    /// <summary>
    /// この画面はローカル対戦とオンライン対戦の両方から来るので、遷移元の画面に戻る。
    /// </summary>
    public void OnBack()
    {
        if (IsNetworked)
        {
            NetSession.Instance.Shutdown();
            NetGameState.Clear();
        }

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
