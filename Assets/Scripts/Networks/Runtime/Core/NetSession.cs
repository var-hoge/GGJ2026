using System;
using System.Net;
using System.Threading.Tasks;
using PhantomCatWorks.RealtimeP2PKit.Lan;
using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit
{
    /// <summary>この端末が対戦でどちら側か。</summary>
    public enum NetRole
    {
        None,

        /// <summary>ロビーを立てた側。乱数シードやゲーム終了など、食い違うと困る決定を担当する。</summary>
        Host,

        /// <summary>ロビーに参加した側。</summary>
        Guest,
    }

    /// <summary>
    /// ゲーム側から見た唯一の通信窓口。通信手段 (LANのTCPか、オンラインのWebRTCか) を
    /// <see cref="ITransport"/> の裏に隠すので、ゲームの同期コードはどちらで繋がっているかを
    /// 一切知らなくてよい。
    ///
    /// 使い方:
    /// <code>
    ///   // 繋ぐ (ローカル対戦)
    ///   NetSession.Instance.StartLanHost("ロビー名", "プレイヤー名");
    ///   await NetSession.Instance.JoinLanLobbyAsync(lobby);
    ///
    ///   // ゲーム側
    ///   NetSession.Instance.RegisterPacketHandler&lt;PlayerStatePacket&gt;(id, OnState);
    ///   NetSession.Instance.Send(id, packet);
    /// </code>
    ///
    /// ソロプレイ時に無駄な GameObject を作らないよう、状態の確認には
    /// <see cref="IsActive"/> (インスタンスを生成しない) を使うこと。
    /// </summary>
    [DisallowMultipleComponent]
    public class NetSession : MonoBehaviour
    {
        private static NetSession _instance;

        // このプロジェクトはドメインリロードを無効にしているため、再生を止めても static が残る。
        // 破棄済みのインスタンスを掴んだままにしないよう明示的に消す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _instance = null;
        }

        /// <summary>必要になった時点で生成される常駐シングルトン。</summary>
        public static NetSession Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject(nameof(NetSession));
                _instance = go.AddComponent<NetSession>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        /// <summary>
        /// 通信対戦中かどうか。インスタンスを生成しないので、ソロプレイ経路の
        /// ガード条件に使っても余計なオブジェクトが増えない。
        /// </summary>
        public static bool IsActive => _instance != null && _instance.IsConnected;

        /// <summary>ホストかどうか。未接続なら false。</summary>
        public static bool IsHost => _instance != null && _instance.Role == NetRole.Host;

        /// <summary>
        /// インスタンスが既にあるか。<see cref="Instance"/> は参照した時点で生成してしまうので、
        /// 後片付け (ハンドラ解除など) では必ずこちらで存在を確かめてから触ること。
        /// </summary>
        public static bool Exists => _instance != null;

        /// <summary>通信路が開いて送受信できるようになった。</summary>
        public event Action Connected;

        /// <summary>通信路が閉じた。相手の切断・回線断・自分からの終了のいずれでも呼ばれる。</summary>
        public event Action<string> Disconnected;

        /// <summary>ロビー一覧が更新された (探索中のみ)。</summary>
        public event Action LobbiesChanged;

        public bool IsConnected => _transport != null && _transport.IsOpen;
        public NetRole Role { get; private set; } = NetRole.None;

        /// <summary>探索中に見つかっているロビー。探索していなければ空。</summary>
        public System.Collections.Generic.IReadOnlyList<LanLobbyInfo> Lobbies =>
            _discovery != null ? _discovery.Lobbies : System.Array.Empty<LanLobbyInfo>();

        private ITransport _transport;
        private PacketRouter _router;
        private LanLobbyDiscovery _discovery;
        private LanLobbyBroadcaster _broadcaster;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _router = new PacketRouter(new MessagePackPayloadCodec());
        }

        private void Update()
        {
            // 裏スレッドで受け取ったものを、ここでメインスレッドのイベントに変える
            _transport?.Poll();
            _broadcaster?.Poll();
            _discovery?.Poll();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        // -----------------------------------------------------------------
        // ロビー探索 (ローカル対戦)
        // -----------------------------------------------------------------

        /// <summary>LAN上のロビーを探し始める。ロビー選択画面を開いている間だけ動かす。</summary>
        public void BeginLanDiscovery()
        {
            if (_discovery != null) return;

            _discovery = new LanLobbyDiscovery();
            _discovery.LobbiesChanged += OnLobbiesChanged;
            if (!_discovery.Start())
            {
                _discovery.LobbiesChanged -= OnLobbiesChanged;
                _discovery.Dispose();
                _discovery = null;
            }
        }

        public void EndLanDiscovery()
        {
            if (_discovery == null) return;
            _discovery.LobbiesChanged -= OnLobbiesChanged;
            _discovery.Dispose();
            _discovery = null;
        }

        private void OnLobbiesChanged() => LobbiesChanged?.Invoke();

        // -----------------------------------------------------------------
        // 接続 (ローカル対戦)
        // -----------------------------------------------------------------

        /// <summary>
        /// ホストとしてロビーを立てる。TCPの待ち受けを始めてから、そのポートを広告する。
        /// 相手が入ってきたら <see cref="Connected"/> が発火する。
        /// </summary>
        public bool StartLanHost(string lobbyName, string playerName)
        {
            if (_transport != null)
            {
                Debug.LogWarning("[RealtimeP2PKit][NetSession] すでに接続処理中のため、ホスト開始を無視しました");
                return false;
            }

            var transport = new LanTcpTransport();
            if (!transport.StartHost(LanConfig.GamePort))
            {
                transport.Dispose();
                return false;
            }

            _broadcaster = new LanLobbyBroadcaster();
            if (!_broadcaster.Start(lobbyName, playerName, transport.ListenPort))
            {
                _broadcaster = null;
                transport.Dispose();
                return false;
            }

            // 自分の広告が自分の一覧に出ないようにする
            if (_discovery != null) _discovery.IgnoreLobbyId = _broadcaster.LobbyId;

            AttachTransport(transport, NetRole.Host);
            return true;
        }

        /// <summary>探索で見つけたロビーへ参加する。</summary>
        public async Task<bool> JoinLanLobbyAsync(LanLobbyInfo lobby)
        {
            if (lobby == null) return false;
            if (_transport != null)
            {
                Debug.LogWarning("[RealtimeP2PKit][NetSession] すでに接続処理中のため、参加を無視しました");
                return false;
            }

            var transport = new LanTcpTransport();
            AttachTransport(transport, NetRole.Guest);

            var ok = await transport.ConnectAsync(lobby.Address, lobby.GamePort);
            if (!ok)
            {
                // 失敗時の Closed は transport 側が積んでいるので、Poll 経由で Disconnected が飛ぶ
                return false;
            }

            return true;
        }

        // -----------------------------------------------------------------
        // トランスポート共通
        // -----------------------------------------------------------------

        /// <summary>
        /// 確立した (あるいはこれから確立する) 通信路を受け取る。
        /// オンライン対戦を再開するときは、P2PManager がここへ WebRtcPeerConnection を渡せばよく、
        /// ゲーム側の同期コードは一切変更しなくて済む。
        /// </summary>
        public void AttachTransport(ITransport transport, NetRole role)
        {
            if (transport == null) return;

            _transport = transport;
            Role = role;

            _transport.Opened += OnTransportOpened;
            _transport.Closed += OnTransportClosed;
            _transport.DataReceived += OnTransportData;

            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][NetSession] トランスポートを接続 role={role} type={transport.GetType().Name}");
        }

        private void OnTransportOpened()
        {
            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][NetSession] 接続しました role={Role}");

            // 相手が入ってきた後も広告を出し続けると、3人目が来てしまう
            StopBroadcasting();
            Connected?.Invoke();
        }

        private void OnTransportClosed(string reason)
        {
            if (P2PLog.ShouldLog(P2PLogLevel.Warn))
                Debug.LogWarning($"[RealtimeP2PKit][NetSession] 切断されました: {reason}");

            Disconnected?.Invoke(reason);
        }

        private void OnTransportData(byte[] payload) => _router.Dispatch(payload);

        // -----------------------------------------------------------------
        // パケット
        // -----------------------------------------------------------------

        /// <summary>packetId ごとに型付きハンドラを登録する。シーンを抜けるときは必ず解除すること。</summary>
        public void RegisterPacketHandler<T>(byte packetId, Action<T> handler) => _router.Register(packetId, handler);

        public void UnregisterPacketHandler(byte packetId) => _router.Unregister(packetId);

        /// <summary>相手へパケットを送る。未接続なら何もしない (ソロプレイでも呼び出し側を分岐させずに済む)。</summary>
        public void Send<T>(byte packetId, T value)
        {
            if (!IsConnected) return;
            _transport.Send(_router.Encode(packetId, value));
        }

        // -----------------------------------------------------------------
        // 終了
        // -----------------------------------------------------------------

        private void StopBroadcasting()
        {
            if (_broadcaster == null) return;
            _broadcaster.Dispose();
            _broadcaster = null;
        }

        /// <summary>接続・広告・探索をすべて畳む。タイトルへ戻るときなどに呼ぶ。</summary>
        public void Shutdown()
        {
            EndLanDiscovery();
            StopBroadcasting();

            if (_transport != null)
            {
                _transport.Opened -= OnTransportOpened;
                _transport.Closed -= OnTransportClosed;
                _transport.DataReceived -= OnTransportData;
                _transport.Dispose();
                _transport = null;
            }

            Role = NetRole.None;
        }
    }
}
