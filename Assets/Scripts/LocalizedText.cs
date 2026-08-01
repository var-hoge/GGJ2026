using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string _englishText;
    [SerializeField] string _japaneseText;
    [SerializeField] string _germanText;

    TextMeshProUGUI _text;

    void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (!LanguageManager.Exist)
        {
            UpdateText(Language.Japanese);
            return;
        }

        LanguageManager.Instance.OnLanguageChanged += UpdateText;
        UpdateText(LanguageManager.Instance.CurrentLanguage);
    }

    void OnDisable()
    {
        if (LanguageManager.Exist)
        {
            LanguageManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    void UpdateText(Language language)
    {
        // ドイツ語訳が未入力のものは英語を代わりに出す (日本語より読める人が多いため)
        _text.text = language switch
        {
            Language.English => _englishText,
            Language.German => !string.IsNullOrEmpty(_germanText) ? _germanText : _englishText,
            _ => _japaneseText,
        };
    }
}
