using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerMove : MonoBehaviour
{
    //まずはプレイヤーのトランスフォームを取得する
    Transform _playerTrs;
    PlayerDirection _playerDirection; //
    float _moveSpeed = 0.1f; //仮で10
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
            // 移動制御
            Vector2 moveDirection = Vector2.zero;
            if (_playerDirection == PlayerDirection.North || _playerDirection == PlayerDirection.South)
            {
                moveDirection = new Vector2(0, Input.GetAxisRaw("Vertical"));
            }
            else if (_playerDirection == PlayerDirection.East || _playerDirection == PlayerDirection.West) 
            {
                moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
            }
            _playerTrs.Translate(moveDirection * _moveSpeed, Space.World);
            // キャッチダイブ
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CatchDive();
            }
            // 多機能ステートマシン
            switch (_playerDirection)
            {
                case PlayerDirection.North:
                    _playerTrs.eulerAngles = new Vector3(0, 0, 0);
                    break;
                case PlayerDirection.South:
                    _playerTrs.eulerAngles = new Vector3(0, 0, 180);
                    break;
                case PlayerDirection.West:
                    _playerTrs.eulerAngles = new Vector3(0, 0, 90);
                    break;
                case PlayerDirection.East:
                    _playerTrs.eulerAngles = new Vector3(0, 0, -90);
                    break;
            }
        }
    }
    // PlayerDirectionの定義
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 readValue = context.ReadValue<Vector2>();
        if (readValue.x > 0) // D入力
        {
            _playerDirection = PlayerDirection.East;
        }
        else if (readValue.x < 0) // A入力
        {
            _playerDirection = PlayerDirection.West;
        }
        else if (readValue.y > 0) // W入力
        {
            _playerDirection = PlayerDirection.North;
        }
        else if (readValue.y < 0) // S入力
        {
            _playerDirection = PlayerDirection.South;
        }
    }
    void CatchDive()
    {
        _isDiving = true;
        Vector3 diveDirection = Vector2.zero;
        Sequence sequence = DOTween.Sequence();
        // 向いている方向にイージングを付けて移動する
        switch (_playerDirection)
        {
            case PlayerDirection.North:
                diveDirection = _playerTrs.position + new Vector3(0, _catchDistance, 0);
                break;
            case PlayerDirection.South:
                diveDirection = _playerTrs.position + new Vector3(0, -_catchDistance, 0);
                break;
            case PlayerDirection.West:
                diveDirection = _playerTrs.position + new Vector3(-_catchDistance, 0, 0);
                break;
            case PlayerDirection.East:
                diveDirection = _playerTrs.position + new Vector3(_catchDistance, 0, 0);;
                break;
        }
        sequence.Append(_playerTrs.DOMove(diveDirection, _catchEndTime));
        sequence.AppendInterval(_intervalTime);
        sequence.AppendCallback(() => _isDiving = false);
    }

}
public enum PlayerDirection
{
    None,
    North,
    South,
    East,
    West
}
public enum CanMoveDirection
{
    None = 1 << 0,
    Up = 1 << 1,
    Down = 1 << 2,
    Right = 1 << 3,
    Left = 1 << 4,
}