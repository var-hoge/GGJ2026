using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LanguageSelector : MonoBehaviour, IMoveHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] GameObject _englishSelectionBar;
    [SerializeField] GameObject _japaneseSelectionBar;
    [SerializeField] GameObject _germanSelectionBar;
    [SerializeField] TitleButtonHighlight _englishHighlight;
    [SerializeField] TitleButtonHighlight _japaneseHighlight;
    [SerializeField] TitleButtonHighlight _germanHighlight;

    [Tooltip("言語名の間の区切り線。選択肢と同じ明るさで点灯・消灯させる")]
    [SerializeField] Image[] _separators;

    /// <summary>画面に並んでいる順。左右キーはこの順で移動する。</summary>
    static readonly Language[] DisplayOrder =
    {
        Language.English,
        Language.Japanese,
        Language.German,
    };

    bool _isFocused;

    TitleButtonHighlight CurrentHighlight => LanguageManager.Instance.CurrentLanguage switch
    {
        Language.English => _englishHighlight,
        Language.German => _germanHighlight,
        _ => _japaneseHighlight,
    };

    void Start()
    {
        UpdateSelectionBar();
        UpdateSeparators();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isFocused = true;
        CurrentHighlight.OnSelect(eventData);
        UpdateSeparators();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isFocused = false;
        CurrentHighlight.OnDeselect(eventData);
        UpdateSeparators();
    }

    /// <summary>
    /// 区切り線は選択対象ではないので TitleButtonHighlight は使わず色だけ合わせる。
    /// (TitleButtonHighlight を付けると区切り線の数だけ選択SEが重なって鳴ってしまう)
    /// </summary>
    void UpdateSeparators()
    {
        var color = _isFocused ? Color.white : TitleButtonHighlight.UnselectedColor;
        foreach (var separator in _separators)
        {
            if (separator != null)
            {
                separator.color = color;
            }
        }
    }

    public void OnMove(AxisEventData eventData)
    {
        var step = eventData.moveDir switch
        {
            MoveDirection.Left => -1,
            MoveDirection.Right => 1,
            _ => 0,
        };
        if (step == 0)
        {
            return;
        }

        // 端で止める。押しっぱなしで意図せず一周してしまうのを避ける
        var index = System.Array.IndexOf(DisplayOrder, LanguageManager.Instance.CurrentLanguage);
        var next = Mathf.Clamp(Mathf.Max(index, 0) + step, 0, DisplayOrder.Length - 1);
        SetLanguage(DisplayOrder[next]);
    }

    public void SelectEnglish()
    {
        Focus();
        SEManager.Instance.Play(SEPath.UI_SELECT);
        SetLanguage(Language.English);
    }

    public void SelectJapanese()
    {
        Focus();
        SEManager.Instance.Play(SEPath.UI_SELECT);
        SetLanguage(Language.Japanese);
    }

    public void SelectGerman()
    {
        Focus();
        SEManager.Instance.Play(SEPath.UI_SELECT);
        SetLanguage(Language.German);
    }

    // マウスでラベルを直接押された場合でも、左右キーの受け口であるこのオブジェクトに選択状態を戻す
    void Focus()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
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
        var current = LanguageManager.Instance.CurrentLanguage;
        _englishSelectionBar.SetActive(current == Language.English);
        _japaneseSelectionBar.SetActive(current == Language.Japanese);
        _germanSelectionBar.SetActive(current == Language.German);
    }
}
