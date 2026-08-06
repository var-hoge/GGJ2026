using System;

namespace PhantomCatWorks.RealtimeP2PKit
{
    /// <summary>
    /// 2者間でバイト列をやり取りする通信路の抽象。
    ///
    /// この層より上 (PacketRouter / IPayloadCodec / NetSession とゲーム側のコード) は
    /// 通信手段を一切知らない。そのため WebRTC (オンライン) と TCP (ローカルLAN) を
    /// 差し替えても、ゲーム側の同期コードは1行も変わらない。
    ///
    /// 実装:
    ///   - WebRtcPeerConnection : オンライン対戦 (STUN + シグナリング経由の直接P2P)
    ///   - Lan.LanTcpTransport  : ローカル対戦 (同一LAN内の直接TCP接続)
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>通信路が開いて送受信可能になった。必ずメインスレッドで発火する。</summary>
        event Action Opened;

        /// <summary>通信路が閉じた。引数は理由の文字列。必ずメインスレッドで発火する。</summary>
        event Action<string> Closed;

        /// <summary>相手からバイト列が届いた。必ずメインスレッドで発火する。</summary>
        event Action<byte[]> DataReceived;

        /// <summary>今すぐ送信できるか。</summary>
        bool IsOpen { get; }

        /// <summary>相手へバイト列を送る。IsOpen が false のときは黙って捨てる。</summary>
        void Send(byte[] payload);

        /// <summary>
        /// 毎フレーム MonoBehaviour の Update から呼ぶ。
        /// 別スレッドで受け取ったデータやイベントを、ここでメインスレッドへ流す。
        /// </summary>
        void Poll();
    }
}
