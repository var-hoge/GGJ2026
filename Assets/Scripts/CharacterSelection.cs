using UnityEngine;

/// <summary>プレイヤーが操作するキャラクター。</summary>
public enum PlayableCharacter
{
    PhantomCat,
    PoliceDog,
}

/// <summary>
/// キャラクター選択画面で選ばれたキャラクターを覚えておく。後続の画面から参照する。
/// </summary>
public static class CharacterSelection
{
    /// <summary>選ばれたキャラクター。まだ選択していなければ null。</summary>
    public static PlayableCharacter? Selected { get; private set; }

    // このプロジェクトはドメインリロードを無効にしているため、再生を止めても static の値が残る。
    // 明示的に消さないと、前回の再生での選択を引き継いでしまう
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Selected = null;
    }

    public static void Select(PlayableCharacter character)
    {
        Selected = character;
    }
}
