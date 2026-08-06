using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit.Lan
{
    /// <summary>
    /// 同一LAN内の2端末を直接つなぐ TCP トランスポート。ホスト側と参加側の両方をこの1クラスが担う
    /// (<see cref="ListenAsync"/> でホスト、<see cref="ConnectAsync"/> で参加)。
    ///
    /// なぜ TCP 単独なのか:
    ///   LAN は RTT が 1ms 未満でパケットロスもほぼ無いため、UDP を併用して信頼性層を自作する
    ///   利点がほとんど無い。一方 TCP なら順序保証と再送が最初から手に入り、捕獲イベントや
    ///   ゲーム終了のような「絶対に落とせない通知」を特別扱いしなくて済む。
    ///   Nagle アルゴリズムだけは 20Hz の位置同期と相性が悪いので NoDelay で切っている。
    ///
    /// ワイヤ形式: [4バイト長 (リトルエンディアン)][本体]
    ///   本体の中身は PacketRouter が付ける [1バイト packetId][MessagePack] で、この層は関知しない。
    ///
    /// スレッド: 受信は裏のタスクで回り、受け取ったものはキューに積むだけ。
    ///   イベントの発火は必ず <see cref="Poll"/> (メインスレッド) で行う。
    /// </summary>
    public class LanTcpTransport : ITransport
    {
        public event Action Opened;
        public event Action<string> Closed;
        public event Action<byte[]> DataReceived;

        /// <summary>壊れた長さフィールドで巨大な確保をしないための上限。</summary>
        private const int MaxFrameBytes = 1 << 20;

        private readonly ConcurrentQueue<byte[]> _inbox = new ConcurrentQueue<byte[]>();
        private readonly object _sendLock = new object();

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        /// <summary>0 = 未通知 / 1 = Poll で Opened を発火する。</summary>
        private int _pendingOpen;

        /// <summary>null 以外なら Poll で Closed を発火する。Interlocked で一度だけ取り出す。</summary>
        private string _pendingClose;

        private volatile bool _isOpen;
        private volatile bool _disposed;

        public bool IsOpen => _isOpen;

        /// <summary>ホストとして実際に待ち受けているポート。参加側へ広告するのに使う。</summary>
        public int ListenPort { get; private set; }

        // -----------------------------------------------------------------
        // 接続の確立
        // -----------------------------------------------------------------

        /// <summary>
        /// ホストとして待ち受け、最初に来た1人だけを受け入れる。
        /// preferredPort が塞がっていたら +1 ずつ最大 portProbeCount 回ずらして試す。
        /// </summary>
        /// <returns>待ち受け開始に成功したか。接続の成立自体は Opened イベントで通知する。</returns>
        public bool StartHost(int preferredPort, int portProbeCount = 10)
        {
            if (_disposed) return false;

            for (var offset = 0; offset < portProbeCount; offset++)
            {
                var port = preferredPort + offset;
                try
                {
                    _listener = new TcpListener(IPAddress.Any, port);
                    _listener.Start();
                    ListenPort = port;
                    break;
                }
                catch (SocketException)
                {
                    _listener = null;
                }
            }

            if (_listener == null)
            {
                if (P2PLog.ShouldLog(P2PLogLevel.Error))
                    Debug.LogError($"[RealtimeP2PKit][LAN] ポート {preferredPort}〜{preferredPort + portProbeCount - 1} がすべて使用中で待ち受けできません");
                return false;
            }

            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][LAN] ホストとして待ち受け開始 port={ListenPort}");

            _cts = new CancellationTokenSource();
            _ = AcceptOnceAsync(_cts.Token);
            return true;
        }

        private async Task AcceptOnceAsync(CancellationToken token)
        {
            try
            {
                var accepted = await _listener.AcceptTcpClientAsync();
                if (token.IsCancellationRequested)
                {
                    accepted.Close();
                    return;
                }

                // 1対1なので、相手が決まったら以降の接続は受け付けない
                StopListener();
                BeginSession(accepted);
            }
            catch (ObjectDisposedException)
            {
                // Dispose 済み。正常な終了経路なので何もしない
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) MarkClosed($"accept failed: {ex.Message}");
            }
        }

        /// <summary>参加側として、広告で見つけたホストへ接続する。</summary>
        public async Task<bool> ConnectAsync(IPAddress address, int port, int timeoutMilliseconds = 5000)
        {
            if (_disposed) return false;

            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][LAN] ホストへ接続します {address}:{port}");

            _cts = new CancellationTokenSource();
            var client = new TcpClient();
            try
            {
                var connectTask = client.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(timeoutMilliseconds, _cts.Token);
                var finished = await Task.WhenAny(connectTask, timeoutTask);
                if (finished != connectTask)
                {
                    client.Close();
                    MarkClosed("接続がタイムアウトしました");
                    return false;
                }

                // 接続自体が失敗していれば、ここで例外が出る
                await connectTask;
                BeginSession(client);
                return true;
            }
            catch (Exception ex)
            {
                client.Close();
                MarkClosed($"接続に失敗しました: {ex.Message}");
                return false;
            }
        }

        private void BeginSession(TcpClient client)
        {
            _client = client;
            _client.NoDelay = true; // 20Hz の小さいパケットが Nagle で束ねられると遅延になる
            _stream = _client.GetStream();
            _isOpen = true;
            Interlocked.Exchange(ref _pendingOpen, 1);

            if (P2PLog.ShouldLog(P2PLogLevel.Info))
                Debug.Log($"[RealtimeP2PKit][LAN] 接続確立 remote={_client.Client.RemoteEndPoint}");

            _ = ReceiveLoopAsync(_cts.Token);
        }

        // -----------------------------------------------------------------
        // 送受信
        // -----------------------------------------------------------------

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var header = new byte[4];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!await ReadExactlyAsync(header, 4, token))
                    {
                        MarkClosed("相手が切断しました");
                        return;
                    }

                    var length = header[0]
                                 | (header[1] << 8)
                                 | (header[2] << 16)
                                 | (header[3] << 24);

                    if (length <= 0 || length > MaxFrameBytes)
                    {
                        MarkClosed($"不正なフレーム長 {length}");
                        return;
                    }

                    var body = new byte[length];
                    if (!await ReadExactlyAsync(body, length, token))
                    {
                        MarkClosed("フレームの途中で切断されました");
                        return;
                    }

                    if (P2PNetworkLog.IsEnabled) Debug.Log(P2PNetworkLogFormat.LanReceive(body));
                    _inbox.Enqueue(body);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) MarkClosed(ex.Message);
            }
        }

        /// <summary>count バイト読み切るまで待つ。相手が閉じたら false。</summary>
        private async Task<bool> ReadExactlyAsync(byte[] buffer, int count, CancellationToken token)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = await _stream.ReadAsync(buffer, offset, count - offset, token);
                if (read <= 0) return false;
                offset += read;
            }

            return true;
        }

        public void Send(byte[] payload)
        {
            if (!_isOpen || payload == null || payload.Length == 0) return;

            if (payload.Length > MaxFrameBytes)
            {
                if (P2PLog.ShouldLog(P2PLogLevel.Error))
                    Debug.LogError($"[RealtimeP2PKit][LAN] 送信データが大きすぎます ({payload.Length} bytes)");
                return;
            }

            var frame = new byte[payload.Length + 4];
            frame[0] = (byte)(payload.Length & 0xFF);
            frame[1] = (byte)((payload.Length >> 8) & 0xFF);
            frame[2] = (byte)((payload.Length >> 16) & 0xFF);
            frame[3] = (byte)((payload.Length >> 24) & 0xFF);
            Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);

            try
            {
                if (P2PNetworkLog.IsEnabled) Debug.Log(P2PNetworkLogFormat.LanSend(payload));
                lock (_sendLock)
                {
                    _stream.Write(frame, 0, frame.Length);
                }
            }
            catch (Exception ex)
            {
                MarkClosed($"送信に失敗しました: {ex.Message}");
            }
        }

        /// <summary>裏のタスクで溜まったものを、ここでメインスレッドのイベントとして流す。</summary>
        public void Poll()
        {
            if (Interlocked.Exchange(ref _pendingOpen, 0) == 1)
            {
                Opened?.Invoke();
            }

            while (_inbox.TryDequeue(out var payload))
            {
                DataReceived?.Invoke(payload);
            }

            var reason = Interlocked.Exchange(ref _pendingClose, null);
            if (reason != null)
            {
                Closed?.Invoke(reason);
            }
        }

        // -----------------------------------------------------------------
        // 終了処理
        // -----------------------------------------------------------------

        /// <summary>切断を記録する。実際の Closed 発火は Poll で行う。最初の理由だけを残す。</summary>
        private void MarkClosed(string reason)
        {
            _isOpen = false;
            Interlocked.CompareExchange(ref _pendingClose, reason ?? "closed", null);
        }

        private void StopListener()
        {
            if (_listener == null) return;
            try { _listener.Stop(); } catch { /* 閉じ済みなら無視してよい */ }
            _listener = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _isOpen = false;

            if (P2PLog.ShouldLog(P2PLogLevel.Info)) Debug.Log("[RealtimeP2PKit][LAN] トランスポートを破棄します");

            try { _cts?.Cancel(); } catch { /* 破棄中の例外は握りつぶす */ }
            StopListener();
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            try { _cts?.Dispose(); } catch { }

            _stream = null;
            _client = null;
            _cts = null;
        }
    }
}
