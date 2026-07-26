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

    Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void Start()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject)
        {
            transform.localScale = Vector3.one;
            _image.color = UnselectedColor;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(Vector3.one * SelectedScale, TweenDuration).SetEase(Ease.OutBack);
        _image.color = Color.white;

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
