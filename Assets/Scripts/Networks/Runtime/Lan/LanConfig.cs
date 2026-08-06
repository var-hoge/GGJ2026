using System;

namespace PhantomCatWorks.RealtimeP2PKit.Lan
{
    /// <summary>ローカル通信の固定パラメータ。両端末で一致している必要がある。</summary>
    public static class LanConfig
    {
        /// <summary>
        /// 広告フォーマットのバージョン。異なるビルド同士が同じLANに居たときに
        /// 誤って繋がらないよう、一致しない広告は無視する。
        /// パケット定義を変えたら必ず上げること。
        /// </summary>
        public const int ProtocolVersion = 1;

        /// <summary>ホストがロビー広告をブロードキャストするポート。</summary>
        public const int DiscoveryPort = 47777;

        /// <summary>ホストが対戦データを待ち受けるポート (塞がっていれば +1 ずつずらす)。</summary>
        public const int GamePort = 47778;

        /// <summary>ロビー広告を送る間隔 (秒)。</summary>
        public const float BroadcastIntervalSeconds = 1f;

        /// <summary>この秒数だけ広告が途絶えたロビーは一覧から消す。</summary>
        public const float LobbyTimeoutSeconds = 3.5f;
    }

    /// <summary>UDPブロードキャストで飛ばすロビー広告の中身。</summary>
    [Serializable]
    public class LanLobbyAdvertisement
    {
        public int protocolVersion;
        public string lobbyId;
        public string lobbyName;
        public string hostName;
        public int gamePort;
    }
}
