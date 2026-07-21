using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string _englishText;
    [SerializeField] string _japaneseText;

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
        _text.text = language == Language.English ? _englishText : _japaneseText;
    }
}
