using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 怪盗猫を操作するときだけ全体の明るさを変える。
/// 警察犬を操作するとき (キャラクター選択を経ていない場合を含む) はシーンで設定した色のまま。
/// </summary>
[RequireComponent(typeof(Light2D))]
public class SelectedCharacterGlobalLight : MonoBehaviour
{
    [Tooltip("怪盗猫を操作するときの色")]
    [SerializeField] Color _phantomCatColor = new Color(90f / 255f, 90f / 255f, 90f / 255f, 1f);

    void Start()
    {
        if (CharacterSelection.Selected != PlayableCharacter.PhantomCat)
        {
            return;
        }

        GetComponent<Light2D>().color = _phantomCatColor;
    }
}
