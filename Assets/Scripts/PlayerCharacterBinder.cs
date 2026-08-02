using UnityEngine;

// CatController は同名のクラスがグローバル名前空間にも存在し (Miura 版)、using で取り込むと
// そちらが優先されて GetComponent が空振りする。InGame で使うのは Kenney 版なので、
// 元の名前と衝突しない別名を付けて取り違えを防ぐ
using KenneyCat = IsoTools.Examples.Kenney.Cat;
using KenneyCatController = IsoTools.Examples.Kenney.CatController;
using KenneyCatSpawner = IsoTools.Examples.Kenney.CatSpawner;
using KenneyPlayerController = IsoTools.Examples.Kenney.PlayerController;

/// <summary>
/// キャラクター選択画面で選ばれたキャラクターだけをこの端末の入力で動かす。
/// 選ばれなかった側は対戦相手が操作するため、こちらの入力からは切り離す。
/// </summary>
public class PlayerCharacterBinder : MonoBehaviour
{
    [Tooltip("ポリスドッグ側の操作。シーンに最初から居るので直接設定する")]
    [SerializeField] KenneyPlayerController _policeDogController;

    [Tooltip("ファントムキャットは実行時生成なので、生成元から受け取る")]
    [SerializeField] KenneyCatSpawner _catSpawner;

    [Tooltip("キャラクター選択を経ずにこの画面へ来たとき (ソロ経路や InGame の直接再生) に操作するキャラクター")]
    [SerializeField] PlayableCharacter _fallbackCharacter = PlayableCharacter.PoliceDog;

    /// <summary>
    /// この端末で操作しているキャラクター。まだ居ない (猫が未生成) なら null。
    /// 頭上マーカーなど、操作対象に追従したいものが参照する。
    /// </summary>
    public Transform ControlledCharacter { get; private set; }

    bool _appliedToPhantomCat;

    void Awake()
    {
        // 猫の生成は CatSpawner の Start なので、Awake のうちに購読しておく
        _catSpawner.PhantomCatSpawned += OnPhantomCatSpawned;
        ApplyToPoliceDog();
    }

    void OnDestroy()
    {
        if (_catSpawner != null)
        {
            _catSpawner.PhantomCatSpawned -= OnPhantomCatSpawned;
        }
    }

    void Update()
    {
        // 生成イベントを取りこぼしても必ず反映されるよう、猫が居れば一度だけ適用する
        if (_appliedToPhantomCat || _catSpawner == null || _catSpawner.PhantomCat == null)
        {
            return;
        }

        ApplyToPhantomCat(_catSpawner.PhantomCat);
    }

    void OnPhantomCatSpawned(GameObject phantomCat)
    {
        ApplyToPhantomCat(phantomCat);
    }

    void ApplyToPhantomCat(GameObject phantomCat)
    {
        _appliedToPhantomCat = true;

        var controlledHere = IsControlledHere(PlayableCharacter.PhantomCat);
        if (controlledHere)
        {
            ControlledCharacter = phantomCat.transform;
        }

        var controller = phantomCat.GetComponent<KenneyCatController>();
        if (controller != null)
        {
            controller.enabled = controlledHere;
        }

        // 操作しないときは従来どおり NPC として自動で動く。
        // 操作するときはランダム移動が入力と競合するので止める
        // (Cat 自体は捕獲判定に使われるため無効化せず、自動移動だけ切る)
        var cat = phantomCat.GetComponent<KenneyCat>();
        if (cat != null)
        {
            cat._autoMove = !controlledHere;
        }
    }

    void ApplyToPoliceDog()
    {
        if (_policeDogController == null)
        {
            return;
        }

        var controlledHere = IsControlledHere(PlayableCharacter.PoliceDog);
        _policeDogController.enabled = controlledHere;
        if (controlledHere)
        {
            ControlledCharacter = _policeDogController.transform;
        }
    }

    /// <summary>
    /// この端末で操作するキャラクターかどうか。操作できるのは常に1体だけ。
    /// </summary>
    bool IsControlledHere(PlayableCharacter character)
    {
        return (CharacterSelection.Selected ?? _fallbackCharacter) == character;
    }
}
