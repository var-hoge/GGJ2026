using UnityEngine;

/// <summary>
/// 対戦中、両端末で同じ乱数列を使うためのシード。
///
/// フェイクキャットと怪盗猫の初期配置は CatSpawner が Random.Range で決めるため、
/// シードを揃えないと2台で配置が完全に食い違う。操作キャラの位置は同期されても、
/// 周囲の猫の並びが違えば「どの猫に紛れているか」が判断できず、
/// 本物を見分けるというこのゲームの根幹が成立しない。
///
/// 値は部屋を作った側 (Session.IsInitiator) が決め、オープニングの
/// 準備完了パケットに載せて相手へ渡す (IntroStoryManager)。
/// InGame へ移ってから使うので、シーンをまたいで持ち回る必要がある。
/// </summary>
public static class MatchRandomSeed
{
    /// <summary>共有された乱数シード。</summary>
    public static int Value { get; private set; }

    /// <summary>シードを受け取っているか。ソロ・ローカル対戦では false のままで、従来どおりランダム配置になる。</summary>
    public static bool HasValue { get; private set; }

    // このプロジェクトはドメインリロードを無効にしているため、再生を止めても static の値が残る。
    // 明示的に消さないと、前回の再生のシードを引き継いで毎回同じ配置になってしまう
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Value = 0;
        HasValue = false;
    }

    public static void Set(int seed)
    {
        Value = seed;
        HasValue = true;
    }

    /// <summary>対戦を抜けるときに呼ぶ。</summary>
    public static void Clear()
    {
        Value = 0;
        HasValue = false;
    }
}
