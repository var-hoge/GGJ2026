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
