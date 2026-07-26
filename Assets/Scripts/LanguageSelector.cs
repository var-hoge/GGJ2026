using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;

public class LanguageSelector : MonoBehaviour, IMoveHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] GameObject _englishSelectionBar;
    [SerializeField] GameObject _japaneseSelectionBar;
    [SerializeField] TitleButtonHighlight _englishHighlight;
    [SerializeField] TitleButtonHighlight _japaneseHighlight;

    bool _isFocused;

    TitleButtonHighlight CurrentHighlight =>
        LanguageManager.Instance.CurrentLanguage == Language.English ? _englishHighlight : _japaneseHighlight;

    void Start()
    {
        UpdateSelectionBar();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isFocused = true;
        CurrentHighlight.OnSelect(eventData);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isFocused = false;
        CurrentHighlight.OnDeselect(eventData);
    }

    public void OnMove(AxisEventData eventData)
    {
        switch (eventData.moveDir)
        {
            case MoveDirection.Left:
                SetLanguage(Language.English);
                break;
            case MoveDirection.Right:
                SetLanguage(Language.Japanese);
                break;
        }
    }

    public void SelectEnglish()
    {
        SEManager.Instance.Play(SEPath.UI_SELECT);
        SetLanguage(Language.English);
    }

    public void SelectJapanese()
    {
        SEManager.Instance.Play(SEPath.UI_SELECT);
        SetLanguage(Language.Japanese);
    }

    void SetLanguage(Language language)
    {
        if (LanguageManager.Instance.CurrentLanguage == language)
        {
            return;
        }

        // フォーカス中は選択言語側のTitleButtonHighlightを付け替えて、選択中の見た目を引き継ぐ
        if (_isFocused)
        {
            CurrentHighlight.OnDeselect(null);
        }

        LanguageManager.Instance.SetLanguage(language);
        UpdateSelectionBar();

        if (_isFocused)
        {
            CurrentHighlight.OnSelect(null);
        }
    }

    void UpdateSelectionBar()
    {
        var isEnglish = LanguageManager.Instance.CurrentLanguage == Language.English;
        _englishSelectionBar.SetActive(isEnglish);
        _japaneseSelectionBar.SetActive(!isEnglish);
    }
}
