using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// この端末で操作しているキャラクターの頭上にマーカーを追従させる。
/// キャンバスは Screen Space - Overlay なので、毎フレーム
/// ワールド座標 → スクリーン座標 → キャンバスのローカル座標へ変換して置き直す。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PlayerPositionMarker : MonoBehaviour
{
    [Tooltip("操作キャラクターを決めている側。ここから追従先を受け取る")]
    [SerializeField] PlayerCharacterBinder _binder;

    [Tooltip("頭のどれだけ上に置くか。キャンバスの単位 (参照解像度基準)")]
    [SerializeField] float _offset = 8f;

    RectTransform _rect;
    RectTransform _parentRect;
    Graphic _graphic;
    Camera _camera;

    // 追従先が変わったときだけ引き直す
    Transform _target;
    Renderer _targetRenderer;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _parentRect = _rect.parent as RectTransform;
        _graphic = GetComponent<Graphic>();
    }

    // キャラクターの移動が反映されたあとの位置に合わせたいので LateUpdate で追う
    void LateUpdate()
    {
        UpdateTarget();

        var camera = ResolveCamera();
        if (_targetRenderer == null || camera == null || _parentRect == null)
        {
            // 猫が生成される前など、追従先がまだ無い間は出さない
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // スプライトは回転しないので、AABB の上端をそのまま頭として使える
        var bounds = _targetRenderer.bounds;
        var head = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, head);
        // Overlay キャンバスなので変換用カメラは null を渡す
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect, screenPoint, null, out var localPoint))
        {
            return;
        }

        // anchoredPosition はアンカー基準なので、キャンバス内の位置からアンカー位置を引く
        var anchorPoint = _parentRect.rect.min + Vector2.Scale(_rect.anchorMin, _parentRect.rect.size);
        _rect.anchoredPosition = localPoint - anchorPoint + new Vector2(0f, _offset);
    }

    void UpdateTarget()
    {
        var target = _binder != null ? _binder.ControlledCharacter : null;
        if (target == _target)
        {
            return;
        }

        _target = target;
        // 頭の高さはスプライトの上端から取る。犬 ("New Sprite") と猫 ("Sprite") で
        // 子オブジェクトの名前が違うので、名前ではなく型で引く
        _targetRenderer = target != null ? target.GetComponentInChildren<SpriteRenderer>() : null;
    }

    Camera ResolveCamera()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        return _camera;
    }

    void SetVisible(bool visible)
    {
        if (_graphic != null && _graphic.enabled != visible)
        {
            _graphic.enabled = visible;
        }
    }
}
