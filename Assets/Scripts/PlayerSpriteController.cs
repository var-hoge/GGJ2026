using IsoTools;
using UnityEngine;

[RequireComponent(typeof(IsoObject))]
public class PlayerSpriteController : MonoBehaviour
{
    [SerializeField] Sprite toRightSprite;
    [SerializeField] Sprite toLeftSprite;
    [SerializeField] SpriteRenderer spriteRenderer;

    private float? prevIsoPositionX = null;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        Vector3 position = transform.position;
        if (prevIsoPositionX != null)
        {
            if (position.x - prevIsoPositionX > 0)
            {
                spriteRenderer.sprite = toRightSprite;
            } else if (position.x - prevIsoPositionX < 0) {
                spriteRenderer.sprite = toLeftSprite;
            }
        }
        prevIsoPositionX = position.x;
    }
}
