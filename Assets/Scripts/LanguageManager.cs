using System;
using UnityEngine;

public enum Language
{
    English,
    Japanese,
}

public class LanguageManager : SingletonBehaviour<LanguageManager>
{
    [SerializeField] Language _currentLanguage = Language.English;

    public event Action<Language> OnLanguageChanged;

    public Language CurrentLanguage => _currentLanguage;

    public void SetLanguage(Language language)
    {
        if (_currentLanguage == language)
        {
            return;
        }

        _currentLanguage = language;
        OnLanguageChanged?.Invoke(language);
    }
}
