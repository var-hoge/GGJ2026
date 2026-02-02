using UnityEngine;

public class CatSpriteController : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite[] _spriteArray;
    [SerializeField] float _updateTimeDistance;

    private int _spriteIndex = 0;
    private int _SpriteIndex{ get => _spriteIndex;
                        set{if (value == _spriteArray.Length){_spriteIndex = 0;} else{_spriteIndex = value;}}}
    private float? prevIsoPositionX = null;
    private float _updateFrameDistance = 60;
    private int _frameCount = 0;
    void Awake(){
        _updateFrameDistance *= _updateTimeDistance;}
    void FixedUpdate()
    {
        Vector3 position = transform.position;
        if (prevIsoPositionX != null)
        {
            if (position.x - prevIsoPositionX > 0) {
                spriteRenderer.flipX = true;
            } else if (position.x - prevIsoPositionX < 0) {
                spriteRenderer.flipX = false;
            }
            // if (Time.frameCount % _updateFrameDistance == 0)
            if (_frameCount > _updateFrameDistance)
            {
                _frameCount = 0;
                _SpriteIndex++;
                spriteRenderer.sprite = _spriteArray[_spriteIndex];
            }
        }
        prevIsoPositionX = position.x;
        _frameCount++;
    }
}
