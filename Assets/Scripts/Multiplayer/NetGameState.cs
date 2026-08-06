using UnityEngine;

/// <summary>
/// 通信対戦で画面をまたいで持ち回る値。
/// キャラクター選択画面で決まって InGame で使われる、という流れなので
/// <see cref="CharacterSelection"/> と同じく static で保持する。
/// </summary>
public static class NetGameState
{
    /// <summary>猫の配置に使う乱数シード。ホストが決めて両端末で共有する。</summary>
    public static int CatSeed { get; private set; }

    /// <summary>シードを受け取っているか。ソロプレイでは false のままで、従来どおりランダム配置になる。</summary>
    public static bool HasCatSeed { get; private set; }

    /// <summary>相手が操作するキャラクター。通信対戦でなければ null。</summary>
    public static PlayableCharacter? OpponentCharacter { get; private set; }

    // このプロジェクトはドメインリロードを無効にしているため、再生を止めても static の値が残る。
    // 明示的に消さないと、前回の再生のシードや相手キャラを引き継いでしまう
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        CatSeed = 0;
        HasCatSeed = false;
        OpponentCharacter = null;
    }

    public static void SetCatSeed(int seed)
    {
        CatSeed = seed;
        HasCatSeed = true;
    }

    public static void SetOpponentCharacter(PlayableCharacter? character)
    {
        OpponentCharacter = character;
    }

    /// <summary>タイトルへ戻るときなど、対戦を抜けるときに呼ぶ。</summary>
    public static void Clear()
    {
        CatSeed = 0;
        HasCatSeed = false;
        OpponentCharacter = null;
    }
}
