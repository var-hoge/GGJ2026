using System.Net;

namespace PhantomCatWorks.RealtimeP2PKit.Lan
{
    /// <summary>
    /// 探索で見つかった、参加できるロビー1件。
    /// IPアドレスは広告の中身ではなく UDP パケットの送信元から取る
    /// (ホストが自分のIPを誤って申告しても、実際に届いた経路の方が確実なため)。
    /// </summary>
    public class LanLobbyInfo
    {
        public string LobbyId;
        public string LobbyName;
        public string HostName;
        public IPAddress Address;
        public int GamePort;

        /// <summary>最後に広告を受け取った時刻 (Time.realtimeSinceStartup)。期限切れ判定に使う。</summary>
        public float LastSeenTime;

        public override string ToString() => $"{LobbyName} ({HostName}) @ {Address}:{GamePort}";
    }
}
