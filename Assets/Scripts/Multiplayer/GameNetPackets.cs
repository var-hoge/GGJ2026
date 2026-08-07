using MessagePack;

/// <summary>
/// このゲームがやり取りするパケットの種類。
/// ライブラリ (RealtimeP2PKit) 側には持たせない。何を送るかはゲーム固有のため。
/// 0〜9 はライブラリのデモが使っているので 10 番から始める。
/// </summary>
public static class GameNetPacketId
{
    /// <summary>キャラクター選択画面で、自分が今どちらを選んでいるか。</summary>
    public const byte CharacterSelection = 10;

    /// <summary>ホストが確定させた対戦開始の合図 (猫の配置シードを含む)。</summary>
    public const byte GameStart = 11;

    /// <summary>操作キャラクターの位置と手持ちライトの向き。</summary>
    public const byte PlayerState = 12;

    /// <summary>決着。どちらの端末も同じ結末の画面へ行くために使う。</summary>
    public const byte GameResult = 13;

    /// <summary>ポリスドッグが猫を捕まえようとした。音を両端末で揃えるために使う。</summary>
    public const byte CatchAttempt = 14;

    /// <summary>「この画面の準備ができた」の通知。両者が揃ってから次の画面へ進む。</summary>
    public const byte SceneReady = 15;
}

/// <summary>
/// <see cref="SceneReadyPacket"/> がどの画面についての通知かを表す。
/// 画面をまたいで同じ packetId を使い回すので、これが無いと
/// 前の画面の通知で次の画面が誤って進んでしまう。
/// </summary>
public static class NetSceneStage
{
    /// <summary>オープニング (IntroStory) を読み終えて InGame へ進む。</summary>
    public const byte Intro = 1;

    /// <summary>エンディングを読み終えて Title へ戻る。</summary>
    public const byte Ending = 2;
}

/// <summary>
/// 選択中のキャラクター。Confirmed が true なら決定済み。
/// 決定前も送るので、相手のカーソルがリアルタイムに動く。
/// </summary>
[MessagePackObject]
public struct CharacterSelectionPacket
{
    /// <summary>PlayableCharacter を byte にしたもの。enum のまま送るとバージョン差で崩れやすい。</summary>
    [Key(0)] public byte Character;

    [Key(1)] public bool Confirmed;

    public PlayableCharacter AsCharacter() => (PlayableCharacter)Character;
}

/// <summary>
/// ホストだけが送る対戦開始通知。
/// CatSeed を両端末で共有しないと、猫50匹の配置が食い違って別のゲームになってしまう。
/// </summary>
[MessagePackObject]
public struct GameStartPacket
{
    [Key(0)] public int CatSeed;
}

/// <summary>
/// 操作キャラクターの状態。20Hz 程度で送り続ける。
/// 位置は IsoObject.position (アイソメトリック座標) をそのまま入れる。
/// transform.position ではないので注意 — IsoTools は物理も描画順もこちらを正とする。
/// </summary>
[MessagePackObject]
public struct PlayerStatePacket
{
    [Key(0)] public float X;
    [Key(1)] public float Y;
    [Key(2)] public float Z;

    /// <summary>手持ちライトのZ回転(度)。ポリスドッグのみ意味を持つ。</summary>
    [Key(3)] public float LightAngle;

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2}) light={LightAngle:F0}";
}

/// <summary>決着の通知。判定した側 (ポリスドッグ側、または時間切れならホスト) だけが送る。</summary>
[MessagePackObject]
public struct GameResultPacket
{
    /// <summary>true = 怪盗猫を捕まえた (VeryHappyEnd) / false = 捕まえられなかった (HappyEnd)。</summary>
    [Key(0)] public bool Caught;
}

/// <summary>
/// ポリスドッグが猫を捕まえようとしたときの通知。
/// 捕獲判定はポリスドッグ側の端末でしか行われず (猫側では CatDetector が無効)、
/// このままでは猫プレイヤーには何の音も鳴らないので、鳴らすべき音を伝える。
/// </summary>
[MessagePackObject]
public struct CatchAttemptPacket
{
    /// <summary>true = 本物の怪盗猫だった / false = フェイク猫だった。</summary>
    [Key(0)] public bool WasPhantom;

    /// <summary>
    /// 外したときに鳴らす SE の番号。ランダムに選ばれるため、
    /// 番号を送らないと両端末で違う音が鳴ってしまう。
    /// </summary>
    [Key(1)] public byte WrongSoundIndex;
}

/// <summary>
/// 「こちらは次の画面へ進む準備ができた」の通知。
/// 相手からも同じ Stage の通知が届いた時点で、両端末が同時に遷移する。
/// </summary>
[MessagePackObject]
public struct SceneReadyPacket
{
    /// <summary>どの画面についての通知か (<see cref="NetSceneStage"/>)。</summary>
    [Key(0)] public byte Stage;
}
