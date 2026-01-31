using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerMove : MonoBehaviour
{
    //まずはプレイヤーのトランスフォームを取得する
    Transform _playerTrs;
    PlayerDirection _playerDirection; //
    float _moveSpeed = 1; //仮で10
    bool _isDiving = false;
    float _intervalTime = 0.5f;
    float _catchEndTime = 1.2f;
    float _catchDistance = 2.5f;
    void Awake()
    {
        _playerTrs = transform;
    }
    
    void Update()
    {
        if (!_isDiving)
        {
            float vertical = 0;
            float horizontal = 0;
            if (_playerDirection == PlayerDirection.Up || _playerDirection == PlayerDirection.Down)
            {
                vertical = Input.GetAxisRaw("Vertical"); //マップ上、上下方向に動ける場合だけここのコードが動くようにする
            }
            if (_playerDirection == PlayerDirection.Left || _playerDirection == PlayerDirection.Right)
            {
                horizontal = Input.GetAxisRaw("Horizontal");
            }
            _playerTrs.Translate(new Vector3(horizontal, vertical) * _moveSpeed); //キー入力で上下左右 //斜め押し対応はどうすれば良いのだろうか？ → 
            // CatchDive
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CatchDive();
            }
        }
    }

    void CatchDive()
    {
        _isDiving = true;
        Vector2 diveDirection = Vector2.zero;
        Sequence sequence = DOTween.Sequence();
        // 向いている方向にイージングを付けて移動する
        switch (_playerDirection)
        {
            case PlayerDirection.Up:
                diveDirection = Vector2.up;
                break;
            case PlayerDirection.Down:
                diveDirection = Vector2.down;
                break;
            case PlayerDirection.Left:
                diveDirection = Vector2.left;
                break;
            case PlayerDirection.Right:
                diveDirection = Vector2.right;
                break;
        }
        sequence.Append(_playerTrs.DOMove(diveDirection, _catchEndTime));
        sequence.AppendInterval(0.1f);
        sequence.AppendCallback(() => _isDiving = false);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Debug.Log("OnMove");
        Vector2 readValue = context.ReadValue<Vector2>();
        if (readValue.x > 0) // D入力
            _playerDirection = PlayerDirection.Right;
        else if (readValue.x < 0) // A入力
            _playerDirection = PlayerDirection.Left;
        else if (readValue.y > 0) // W入力
            _playerDirection = PlayerDirection.Up;
        else if (readValue.y < 0) // S入力
            _playerDirection = PlayerDirection.Down;
    }
}
public enum PlayerDirection
{
    Up,
    Down,
    Right,
    Left
}
public enum CanMoveDirection
{
    None = 1 << 0,
    Up = 1 << 1,
    Down = 1 << 2,
    Right = 1 << 3,
    Left = 1 << 4,
}