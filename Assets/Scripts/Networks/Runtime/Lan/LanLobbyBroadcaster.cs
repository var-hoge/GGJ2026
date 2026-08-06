using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit.Lan
{
    /// <summary>
    /// ホスト側。「ここにロビーがあります」を一定間隔でLAN全体へブロードキャストする。
    /// スレッドは使わず、<see cref="Poll"/> が呼ばれた回数で間隔を測って送るだけ。
    /// </summary>
    public class LanLobbyBroadcaster : IDisposable
    {
        private UdpClient _udp;
        private byte[] _payload;
        private IPEndPoint _target;
        private float _nextSendTime;
        private bool _disposed;

        /// <summary>広告しているロビーのID。参加側が自分自身の広告を弾くのにも使う。</summary>
        public string LobbyId { get; private set; }

        /// <param name="gamePort">LanTcpTransport が実際に待ち受けているポート。</param>
        public bool Start(string lobbyName, string hostName, int gamePort)
        {
            if (_disposed) return false;

            LobbyId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var advertisement = new LanLobbyAdvertisement
            {
                protocolVersion = LanConfig.ProtocolVersion,
                lobbyId = LobbyId,
                lobbyName = lobbyName,
                hostName = hostName,
                gamePort = gamePort,
            };

            var json = JsonConvert.SerializeObject(advertisement);
            _payload = Encoding.UTF8.GetBytes(json);
            _target = new IPEndPoint(IPAddress.Broadcast, LanConfig.DiscoveryPort);

            try
            {
                // ポート0でバインドするとOSが空きポートを割り当てる。送信専用なので固定しなくてよい
                _udp = new UdpClient(0) { EnableBroadcast = true };
            }
            catch (Exception ex)
            {
                if (P2PLog.ShouldLog(P2PLogLevel.Error))
                    Debug.LogError($"[RealtimeP2PKit][LAN] 広告用ソケットを開けません: {ex.Message}");
                return false;
            }

            _nextSendTime = 0f; // 最初の Poll で即座に1回送る
            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][LAN] ロビー広告を開始 lobbyId={LobbyId} name={lobbyName} gamePort={gamePort}");

            return true;
        }

        /// <summary>毎フレーム呼ぶ。間隔が来ていれば1回ブロードキャストする。</summary>
        public void Poll()
        {
            if (_disposed || _udp == null || _payload == null) return;
            if (Time.realtimeSinceStartup < _nextSendTime) return;

            _nextSendTime = Time.realtimeSinceStartup + LanConfig.BroadcastIntervalSeconds;

            try
            {
                _udp.Send(_payload, _payload.Length, _target);
                if (P2PNetworkLog.IsEnabled)
                    Debug.Log(P2PNetworkLogFormat.LanDiscovery("->", $"advertise lobbyId={LobbyId}"));
            }
            catch (Exception ex)
            {
                // 一時的にネットワークが落ちている等。次の間隔で再挑戦すればよいので止めない
                if (P2PLog.ShouldLog(P2PLogLevel.Warn))
                    Debug.LogWarning($"[RealtimeP2PKit][LAN] 広告の送信に失敗 (次回再試行): {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (P2PLog.ShouldLog(P2PLogLevel.Info)) Debug.Log("[RealtimeP2PKit][LAN] ロビー広告を停止します");
            try { _udp?.Close(); } catch { /* 閉じ済みなら無視してよい */ }
            _udp = null;
            _payload = null;
        }
    }
}
