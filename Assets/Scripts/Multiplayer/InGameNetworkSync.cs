using IsoTools;
using IsoTools.Physics;
using KanKikuchi.AudioManager;
using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// InGame での相手キャラクターの同期。
///
/// 権威の持ち方:
///   - 位置と手持ちライトの向きは「そのキャラクターを操作している端末」が正。
///     お互いに自分の分だけを送り合うので、取り合いが起きない。
///   - 捕獲の判定はポリスドッグ側の端末だけが行う (CatDetector が猫側では無効化されているため)。
///   - 時間切れの宣言はホストだけが行う (GameManager 側で分岐)。
///
/// 位置は transform ではなく <see cref="IsoObject.position"/> を送る。IsoTools は
/// 描画順も物理もこちらを正としているため、transform を直接書くと表示が崩れる。
///
/// 通信対戦のときだけ、InGame シーンの読み込み時に自動で生成される
/// (<see cref="Install"/>)。ソロプレイでは一切登場しない。
/// </summary>
public class InGameNetworkSync : MonoBehaviour
{
    [Tooltip("操作キャラクターを決めている側。自動生成時は実行時に探して入る")]
    [SerializeField] PlayerCharacterBinder _binder;

    [Tooltip("自分の状態を送る間隔 (秒)。0.05 = 20Hz")]
    [SerializeField] float _sendIntervalSeconds = 0.05f;

    [Tooltip("相手の位置へ寄せる速さ。大きいほど機敏だがガタつきやすい")]
    [SerializeField] float _remoteLerpSpeed = 12f;

    [Tooltip("相手が切断したときに戻る画面")]
    [SerializeField] string _disconnectScene = "Title";

    IsoObject _localIso;
    Transform _localLight;

    IsoObject _remoteIso;
    IsoRigidbody _remoteRigidbody;
    Transform _remoteLight;

    bool _resolved;
    float _sendTimer;

    bool _hasRemoteTarget;
    Vector3 _remoteTargetPosition;
    float _remoteTargetLightAngle;

    /// <summary>
    /// InGame を読み込んだときに、通信対戦なら自分自身をシーンへ差し込む。
    /// こうしておくと InGame.unity 側に手を入れなくてよく、ソロプレイの経路も一切変わらない。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "InGame") return;
        if (!NetSession.IsActive) return;

        // 手動でシーンに置かれている場合は二重に作らない
        if (FindFirstObjectByType<InGameNetworkSync>() != null) return;

        var binder = FindFirstObjectByType<PlayerCharacterBinder>();
        if (binder == null)
        {
            Debug.LogError("PlayerCharacterBinder が見つからないため、対戦相手の同期を開始できません");
            return;
        }

        var go = new GameObject(nameof(InGameNetworkSync));
        var sync = go.AddComponent<InGameNetworkSync>();
        sync._binder = binder;
    }

    void OnEnable()
    {
        if (!NetSession.IsActive) return;

        var session = NetSession.Instance;
        session.RegisterPacketHandler<PlayerStatePacket>(GameNetPacketId.PlayerState, OnRemoteState);
        session.RegisterPacketHandler<GameResultPacket>(GameNetPacketId.GameResult, OnRemoteResult);
        session.RegisterPacketHandler<CatchAttemptPacket>(GameNetPacketId.CatchAttempt, OnRemoteCatchAttempt);
        session.Disconnected += OnDisconnected;
    }

    void OnDisable()
    {
        if (!NetSession.Exists) return;

        var session = NetSession.Instance;
        session.UnregisterPacketHandler(GameNetPacketId.PlayerState);
        session.UnregisterPacketHandler(GameNetPacketId.GameResult);
        session.UnregisterPacketHandler(GameNetPacketId.CatchAttempt);
        session.Disconnected -= OnDisconnected;
    }

    void Update()
    {
        // 怪盗猫は CatSpawner が実行時に生成するので、両方が揃うまで毎フレーム探す
        if (!_resolved) TryResolve();

        SendLocalState();
        ApplyRemoteState();
    }

    void TryResolve()
    {
        if (_binder == null) return;

        var local = _binder.ControlledCharacter;
        var remote = _binder.RemoteCharacter;
        if (local == null || remote == null) return;

        _localIso = local.GetComponent<IsoObject>();
        _remoteIso = remote.GetComponent<IsoObject>();
        _remoteRigidbody = remote.GetComponent<IsoRigidbody>();

        // 手持ちライトはポリスドッグにしか無い。犬がどちら側かで送る/受けるが決まる
        var dog = _binder.PoliceDog;
        if (dog != null && dog.HandLight != null)
        {
            if (local == dog.transform) _localLight = dog.HandLight;
            else if (remote == dog.transform) _remoteLight = dog.HandLight;
        }

        if (_localIso == null || _remoteIso == null)
        {
            Debug.LogError("IsoObject が見つからないため、位置を同期できません");
            return;
        }

        _resolved = true;
    }

    void SendLocalState()
    {
        if (!_resolved || !NetSession.IsActive) return;

        _sendTimer += Time.deltaTime;
        if (_sendTimer < _sendIntervalSeconds) return;
        _sendTimer = 0f;

        var position = _localIso.position;
        NetSession.Instance.Send(GameNetPacketId.PlayerState, new PlayerStatePacket
        {
            X = position.x,
            Y = position.y,
            Z = position.z,
            LightAngle = _localLight != null ? _localLight.rotation.eulerAngles.z : 0f,
        });
    }

    void OnRemoteState(PlayerStatePacket packet)
    {
        _remoteTargetPosition = new Vector3(packet.X, packet.Y, packet.Z);
        _remoteTargetLightAngle = packet.LightAngle;
        _hasRemoteTarget = true;
    }

    void ApplyRemoteState()
    {
        if (!_resolved || !_hasRemoteTarget) return;

        // 相手のキャラクターは操作コンポーネントを切ってあるが、IsoRigidbody に
        // 慣性が残っていると届いた位置と引っ張り合うので、毎フレーム打ち消す
        if (_remoteRigidbody != null) _remoteRigidbody.velocity = Vector3.zero;

        _remoteIso.position = Vector3.Lerp(
            _remoteIso.position,
            _remoteTargetPosition,
            Time.deltaTime * _remoteLerpSpeed);

        if (_remoteLight != null)
        {
            _remoteLight.rotation = Quaternion.Euler(0f, 0f, _remoteTargetLightAngle);
        }
    }

    /// <summary>
    /// ポリスドッグ側が捕獲を試みたことの通知。
    /// 捕獲判定は犬側の端末でしか走らないため、猫プレイヤーの端末では
    /// この通知を受けて初めて音が鳴る。
    /// </summary>
    void OnRemoteCatchAttempt(CatchAttemptPacket packet)
    {
        if (packet.WasPhantom)
        {
            SEManager.Instance.Play(SEPath.SFX_GAME_CORRECT);
            return;
        }

        var sounds = IsoTools.Examples.Kenney.CatDetector.WrongSounds;
        var index = Mathf.Clamp(packet.WrongSoundIndex, 0, sounds.Length - 1);
        SEManager.Instance.Play(sounds[index]);
    }

    /// <summary>相手の端末が下した決着を、こちらでも同じ画面へ反映する。</summary>
    void OnRemoteResult(GameResultPacket packet)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager が無いため、決着を反映できません");
            return;
        }

        GameManager.Instance.ApplyRemoteResult(packet.Caught);
    }

    void OnDisconnected(string reason)
    {
        Debug.LogWarning($"対戦相手との接続が切れました: {reason}");
        NetSession.Instance.Shutdown();
        NetGameState.Clear();
        SceneManager.LoadScene(_disconnectScene);
    }
}
