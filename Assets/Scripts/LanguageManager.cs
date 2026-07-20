using UnityEngine;

public enum Language
{
    English,
    Japanese,
}

public class LanguageManager : SingletonBehaviour<LanguageManager>
{
    [SerializeField] Language _currentLanguage = Language.English;

    public Language CurrentLanguage => _currentLanguage;

    public void SetLanguage(Language language)
    {
        _currentLanguage = language;
    }
}
