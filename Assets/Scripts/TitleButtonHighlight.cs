using DG.Tweening;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    static readonly Color UnselectedColor = new Color(0.69f, 0.69f, 0.69f, 1f);
    const float SelectedScale = 1.15f;
    const float TweenDuration = 0.15f;

    [Header("選択したときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] float _selectRumbleStrength = 0.15f;
    [SerializeField] float _selectRumbleDuration = 0.05f;

    [Header("選択中だけ表示するバー (使わないボタンでは未設定でよい)")]
    [SerializeField] GameObject _selectionBar;

    Image _image;

    bool IsSelected =>
        EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    // 非表示のオブジェクトには OnDeselect が届かないので、表示に戻った時点の選択状態から見た目を作り直す
    void OnEnable()
    {
        var selected = IsSelected;
        transform.DOKill();
        transform.localScale = selected ? Vector3.one * SelectedScale : Vector3.one;
        _image.color = selected ? Color.white : UnselectedColor;
        SetSelectionBarActive(selected);
    }

    public void OnSelect(BaseEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(Vector3.one * SelectedScale, TweenDuration).SetEase(Ease.OutBack);
        _image.color = Color.white;
        SetSelectionBarActive(true);

        // シーン開始直後の初期選択では鳴らさない
        if (Time.timeSinceLevelLoad > 0.5f)
        {
            SEManager.Instance.Play(SEPath.UI_MOVE);
            GamepadRumble.Play(_selectRumbleStrength, _selectRumbleDuration);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(Vector3.one, TweenDuration);
        _image.color = UnselectedColor;
        SetSelectionBarActive(false);
    }

    void SetSelectionBarActive(bool active)
    {
        if (_selectionBar != null)
        {
            _selectionBar.SetActive(active);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // マウスホバーでも選択状態を移し、パッドと状態を一本化する。
        // 言語ラベルのようにキー操作の対象外のボタンは、代わりに親のSelectable(言語選択の行)へフォーカスを渡す
        var target = FindNavigableSelectable();
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }

    /// <summary>自分から親へ辿って、キー操作でフォーカスできる最初のSelectableを返す。</summary>
    Selectable FindNavigableSelectable()
    {
        for (var current = transform; current != null; current = current.parent)
        {
            var selectable = current.GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None)
            {
                return selectable;
            }
        }

        return null;
    }

    void OnDestroy()
    {
        transform.DOKill();
    }
}
