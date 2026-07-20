using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

/// <summary>
/// 接続中のコントローラーの種類に応じて、操作説明のアイコンを切り替える。
/// コントローラー未接続時はキーボード用のアイコンを表示する。
/// 複数スプライトを指定した場合は1行 maxIconsPerRow 個で折り返し、
/// 2行目以降は基準位置から上方向へ積むグリッド状に表示する。
/// (下方向へ伸ばすと後続の操作説明と重なるため上方向へ伸ばす)
/// 説明テキストの水平位置は変わらないため、画面端でも見切れない。
/// </summary>
public class ControllerIconMapper : MonoBehaviour
{
    [SerializeField] private Image iconImage = null;
    [SerializeField] private RectTransform textRect = null;
    [SerializeField] private float iconSpacing = 22f;
    [SerializeField] private int maxIconsPerRow = 2;

    [Header("コントローラー種別ごとのアイコン(複数指定でグリッド表示)")]
    [SerializeField] private Sprite[] switchSprites = null;
    [SerializeField] private Sprite[] xboxSprites = null;
    [SerializeField] private Sprite[] ps5Sprites = null;
    [SerializeField] private Sprite[] keyboardSprites = null;

    private readonly List<Image> extraIcons = new List<Image>();
    private float baseIconX;
    private float baseTextX;
    private bool basePositionsCached;

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        UpdateIcons();
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        UpdateIcons();
    }

    private void UpdateIcons()
    {
        var sprites = SelectSprites(Gamepad.current);
        if (sprites == null || sprites.Length == 0) return;

        iconImage.sprite = sprites[0];

        // 2枚目以降は動的に生成したアイコンをグリッド状に並べる
        for (var i = 0; i < sprites.Length - 1; i++)
        {
            if (i >= extraIcons.Count)
            {
                extraIcons.Add(CreateExtraIcon(i));
            }
            extraIcons[i].sprite = sprites[i + 1];
            extraIcons[i].gameObject.SetActive(true);
        }
        for (var i = sprites.Length - 1; i < extraIcons.Count; i++)
        {
            extraIcons[i].gameObject.SetActive(false);
        }

        if (!basePositionsCached)
        {
            baseIconX = iconImage.rectTransform.anchoredPosition.x;
            if (textRect != null)
            {
                baseTextX = textRect.anchoredPosition.x;
            }
            basePositionsCached = true;
        }

        // アイコンの起点を列数分だけ左へずらし、グリッドを左・上方向へ伸ばす。
        // テキストはアイコンの子のため、同量だけ右へ戻して画面上の位置を保つ。
        var columns = Mathf.Min(sprites.Length, Mathf.Max(1, maxIconsPerRow));
        var shift = (columns - 1) * iconSpacing;
        var iconPos = iconImage.rectTransform.anchoredPosition;
        iconPos.x = baseIconX - shift;
        iconImage.rectTransform.anchoredPosition = iconPos;

        if (textRect != null)
        {
            var textPos = textRect.anchoredPosition;
            textPos.x = baseTextX + shift;
            textRect.anchoredPosition = textPos;
        }
    }

    private Image CreateExtraIcon(int index)
    {
        var go = new GameObject($"Icon{index + 2}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = iconImage.gameObject.layer;

        var rect = (RectTransform)go.transform;
        rect.SetParent(iconImage.rectTransform, false);
        rect.sizeDelta = iconImage.rectTransform.sizeDelta;

        // 基準アイコンを含めた通し番号から列・行を決める(行は上方向へ)
        var slot = index + 1;
        var columnsPerRow = Mathf.Max(1, maxIconsPerRow);
        rect.anchoredPosition = new Vector2(
            slot % columnsPerRow * iconSpacing,
            slot / columnsPerRow * iconSpacing);

        var image = go.GetComponent<Image>();
        image.preserveAspect = iconImage.preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private Sprite[] SelectSprites(Gamepad gamepad)
    {
        switch (gamepad)
        {
            case null:                     return keyboardSprites;
            case SwitchProControllerHID _: return switchSprites;
            case XInputController _:       return xboxSprites;
            case DualShockGamepad _:       return ps5Sprites;
            default:                       return ps5Sprites; // 不明なパッドは従来通りPS5表記
        }
    }
}
