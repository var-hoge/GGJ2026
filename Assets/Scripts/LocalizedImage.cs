using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LocalizedImage : MonoBehaviour
{
    // 文字スプライトはどれも同じ文字サイズで書き出されているので、共通の倍率でピクセル数からRectのサイズを決めると
    // 言語を切り替えても文字の大きさと線の太さが揃う。切り替わらない文字画像もシーン側でこの倍率に合わせてある
    public const float UnitsPerPixel = 0.19f;

    [SerializeField] Sprite _englishSprite;
    [SerializeField] Sprite _japaneseSprite;

    Image _image;
    RectTransform _rectTransform;

    void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = (RectTransform)transform;
    }

    void OnEnable()
    {
        if (!LanguageManager.Exist)
        {
            UpdateSprite(Language.Japanese);
            return;
        }

        LanguageManager.Instance.OnLanguageChanged += UpdateSprite;
        UpdateSprite(LanguageManager.Instance.CurrentLanguage);
    }

    void OnDisable()
    {
        if (LanguageManager.Exist)
        {
            LanguageManager.Instance.OnLanguageChanged -= UpdateSprite;
        }
    }

    void UpdateSprite(Language language)
    {
        var sprite = language == Language.English ? _englishSprite : _japaneseSprite;
        _image.sprite = sprite;

        if (sprite != null)
        {
            _rectTransform.sizeDelta = sprite.rect.size * UnitsPerPixel;
        }
    }
}
