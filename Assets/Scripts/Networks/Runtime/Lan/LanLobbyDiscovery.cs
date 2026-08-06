using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit.Lan
{
    /// <summary>
    /// 参加側。LAN に流れてくるロビー広告を拾って一覧を作る。
    /// 一定時間広告が途絶えたロビーは自動で消えるので、ホストが落ちた場合も一覧に残らない。
    ///
    /// 受信は裏のタスクで回し、<see cref="Poll"/> でメインスレッドへ流す。
    /// </summary>
    public class LanLobbyDiscovery : IDisposable
    {
        /// <summary>一覧の中身が変わったときだけ発火する (毎フレームの広告受信では発火しない)。</summary>
        public event Action LobbiesChanged;

        private readonly ConcurrentQueue<(IPAddress address, byte[] payload)> _inbox =
            new ConcurrentQueue<(IPAddress, byte[])>();

        private readonly Dictionary<string, LanLobbyInfo> _lobbies = new Dictionary<string, LanLobbyInfo>();
        private readonly List<LanLobbyInfo> _sorted = new List<LanLobbyInfo>();
        private readonly List<string> _expiredBuffer = new List<string>();

        private UdpClient _udp;
        private CancellationTokenSource _cts;
        private bool _disposed;

        /// <summary>自分がホストのときに、自分自身の広告を一覧から除くためのID。</summary>
        public string IgnoreLobbyId { get; set; }

        /// <summary>見つかっているロビー。広告を最初に受け取った順に並ぶ。</summary>
        public IReadOnlyList<LanLobbyInfo> Lobbies => _sorted;

        public bool Start()
        {
            if (_disposed) return false;

            try
            {
                _udp = new UdpClient();
                // 同一マシンで2つ起動したときに、両方が同じポートを開けるようにする
                _udp.ExclusiveAddressUse = false;
                _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udp.Client.Bind(new IPEndPoint(IPAddress.Any, LanConfig.DiscoveryPort));
            }
            catch (Exception ex)
            {
                // macOS/Linux では SO_REUSEADDR だけでは同一ポートの複数バインドが通らないことがある。
                // 1台で2インスタンス動かして検証しているときに起きるので、原因が分かる文言にしておく
                if (P2PLog.ShouldLog(P2PLogLevel.Error))
                {
                    Debug.LogError(
                        $"[RealtimeP2PKit][LAN] ロビー探索用ポート {LanConfig.DiscoveryPort} を開けません: {ex.Message}\n" +
                        "同じPCで2つ起動して検証している場合、片方は探索せずに先にロビーを作成してください。");
                }

                _udp = null;
                return false;
            }

            _cts = new CancellationTokenSource();
            _ = ReceiveLoopAsync(_cts.Token);

            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][LAN] ロビー探索を開始 port={LanConfig.DiscoveryPort}");

            return true;
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var result = await _udp.ReceiveAsync();
                    _inbox.Enqueue((result.RemoteEndPoint.Address, result.Buffer));
                }
            }
            catch (ObjectDisposedException)
            {
                // Dispose 済み。正常な終了経路
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested && P2PLog.ShouldLog(P2PLogLevel.Warn))
                    Debug.LogWarning($"[RealtimeP2PKit][LAN] 探索の受信が止まりました: {ex.Message}");
            }
        }

        /// <summary>毎フレーム呼ぶ。受信した広告の取り込みと、期限切れロビーの削除を行う。</summary>
        public void Poll()
        {
            if (_disposed) return;

            var changed = false;

            while (_inbox.TryDequeue(out var received))
            {
                changed |= Ingest(received.address, received.payload);
            }

            changed |= RemoveExpired();

            if (changed)
            {
                RebuildSorted();
                LobbiesChanged?.Invoke();
            }
        }

        /// <returns>一覧の構成が変わったか (既存ロビーの時刻更新だけなら false)。</returns>
        private bool Ingest(IPAddress address, byte[] payload)
        {
            LanLobbyAdvertisement ad;
            try
            {
                ad = JsonConvert.DeserializeObject<LanLobbyAdvertisement>(Encoding.UTF8.GetString(payload));
            }
            catch (Exception ex)
            {
                if (P2PLog.ShouldLog(P2PLogLevel.Verbose))
                    Debug.Log($"[RealtimeP2PKit][LAN] 解釈できない広告を無視しました: {ex.Message}");
                return false;
            }

            if (ad == null || string.IsNullOrEmpty(ad.lobbyId)) return false;

            // 別バージョンのビルドが同じLANに居ても混ざらないようにする
            if (ad.protocolVersion != LanConfig.ProtocolVersion)
            {
                if (P2PLog.ShouldLog(P2PLogLevel.Verbose))
                    Debug.Log($"[RealtimeP2PKit][LAN] プロトコル違いの広告を無視 (相手={ad.protocolVersion} 自分={LanConfig.ProtocolVersion})");
                return false;
            }

            // 自分がホストなら、自分の広告は一覧に出さない
            if (!string.IsNullOrEmpty(IgnoreLobbyId) && ad.lobbyId == IgnoreLobbyId) return false;

            if (P2PNetworkLog.IsEnabled)
                Debug.Log(P2PNetworkLogFormat.LanDiscovery("<-", $"lobbyId={ad.lobbyId} from={address}"));

            if (_lobbies.TryGetValue(ad.lobbyId, out var existing))
            {
                // 既知のロビー。生存時刻を延ばすだけなので一覧の作り直しは要らない
                existing.LastSeenTime = Time.realtimeSinceStartup;
                existing.Address = address;
                existing.GamePort = ad.gamePort;
                return false;
            }

            _lobbies[ad.lobbyId] = new LanLobbyInfo
            {
                LobbyId = ad.lobbyId,
                LobbyName = string.IsNullOrEmpty(ad.lobbyName) ? "ロビー" : ad.lobbyName,
                HostName = ad.hostName,
                Address = address,
                GamePort = ad.gamePort,
                LastSeenTime = Time.realtimeSinceStartup,
            };

            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][LAN] ロビーを発見: {_lobbies[ad.lobbyId]}");

            return true;
        }

        private bool RemoveExpired()
        {
            var deadline = Time.realtimeSinceStartup - LanConfig.LobbyTimeoutSeconds;

            _expiredBuffer.Clear();
            foreach (var pair in _lobbies)
            {
                if (pair.Value.LastSeenTime < deadline) _expiredBuffer.Add(pair.Key);
            }

            foreach (var lobbyId in _expiredBuffer)
            {
                if (P2PLog.ShouldLog(P2PLogLevel.Info))
                    Debug.Log($"[RealtimeP2PKit][LAN] ロビーが消えました: {_lobbies[lobbyId]}");
                _lobbies.Remove(lobbyId);
            }

            return _expiredBuffer.Count > 0;
        }

        private void RebuildSorted()
        {
            _sorted.Clear();
            foreach (var lobby in _lobbies.Values) _sorted.Add(lobby);
            // 表示順が毎フレーム入れ替わらないよう、名前で安定させる
            _sorted.Sort((a, b) => string.CompareOrdinal(a.LobbyId, b.LobbyId));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (P2PLog.ShouldLog(P2PLogLevel.Info)) Debug.Log("[RealtimeP2PKit][LAN] ロビー探索を停止します");
            try { _cts?.Cancel(); } catch { /* 破棄中の例外は握りつぶす */ }
            try { _udp?.Close(); } catch { }
            try { _cts?.Dispose(); } catch { }

            _udp = null;
            _cts = null;
            _lobbies.Clear();
            _sorted.Clear();
        }
    }
}
