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
            SEManager.Instance.Play(SEPath.SYSTEM20);
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
        // マウスホバーでも選択状態を移し、パッドと状態を一本化する
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    void OnDestroy()
    {
        transform.DOKill();
    }
}
