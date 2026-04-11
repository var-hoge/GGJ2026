using System.Collections;
using IsoTools;
using UnityEngine;

[RequireComponent(typeof(IsoObject))]
public class PlayerSpriteController : MonoBehaviour
{
    [SerializeField] Sprite toRightSprite;
    [SerializeField] Sprite toLeftSprite;
    [SerializeField] SpriteRenderer spriteRenderer;

    private float? prevIsoPositionX = null;

    private float _waitTime = 0;

    private static readonly float DEFAULT_WAIT_TIME = 0.5f;

    void Start()
    {
        StartCoroutine(ChangeSprite());
    }

    void Update()
    {
        if (_waitTime > 0)
        {
            _waitTime = Mathf.Max(0, _waitTime - Time.deltaTime);
        }
    }

    private IEnumerator ChangeSprite()
    {
        var isRight = true;
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            if (_waitTime <= 0)
            {
                spriteRenderer.sprite = isRight ? toRightSprite : toLeftSprite;
                isRight = !isRight;   
            }
        }
    }

    void FixedUpdate()
    {
        Vector3 position = transform.position;
        if (prevIsoPositionX != null)
        {
            var diff = position.x - prevIsoPositionX;
            if (diff > 0)
            {
                spriteRenderer.sprite = toRightSprite;
            } else if (diff < 0) {
                spriteRenderer.sprite = toLeftSprite;
            }

            if (diff != 0)
            {
                 _waitTime = DEFAULT_WAIT_TIME;   
            }
        }
        prevIsoPositionX = position.x;
    }
}
