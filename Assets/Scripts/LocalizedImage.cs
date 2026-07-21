using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LocalizedImage : MonoBehaviour
{
    [SerializeField] Sprite _englishSprite;
    [SerializeField] Sprite _japaneseSprite;

    Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void OnEnable()
    {
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
        _image.sprite = language == Language.English ? _englishSprite : _japaneseSprite;
    }
}
