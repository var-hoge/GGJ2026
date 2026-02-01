using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerMove : MonoBehaviour
{
    //まずはプレイヤーのトランスフォームを取得する
    Transform _playerTrs;
    CharacterDirection _characterDirection; //
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
            if (_characterDirection == CharacterDirection.North || _characterDirection == CharacterDirection.South)
            {
                moveDirection = new Vector2(0, Input.GetAxisRaw("Vertical"));
            }
            else if (_characterDirection == CharacterDirection.East || _characterDirection == CharacterDirection.West) 
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
            switch (_characterDirection)
            {
                case CharacterDirection.North:
                    _playerTrs.eulerAngles = new Vector3(0, 0, 0);
                    break;
                case CharacterDirection.South:
                    _playerTrs.eulerAngles = new Vector3(0, 0, 180);
                    break;
                case CharacterDirection.West:
                    _playerTrs.eulerAngles = new Vector3(0, 0, 90);
                    break;
                case CharacterDirection.East:
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
            _characterDirection = CharacterDirection.East;
        }
        else if (readValue.x < 0) // A入力
        {
            _characterDirection = CharacterDirection.West;
        }
        else if (readValue.y > 0) // W入力
        {
            _characterDirection = CharacterDirection.North;
        }
        else if (readValue.y < 0) // S入力
        {
            _characterDirection = CharacterDirection.South;
        }
    }
    void CatchDive()
    {
        _isDiving = true;
        Vector3 diveDirection = Vector2.zero;
        Sequence sequence = DOTween.Sequence();
        // 向いている方向にイージングを付けて移動する
        switch (_characterDirection)
        {
            case CharacterDirection.North:
                diveDirection = _playerTrs.position + new Vector3(0, _catchDistance, 0);
                break;
            case CharacterDirection.South:
                diveDirection = _playerTrs.position + new Vector3(0, -_catchDistance, 0);
                break;
            case CharacterDirection.West:
                diveDirection = _playerTrs.position + new Vector3(-_catchDistance, 0, 0);
                break;
            case CharacterDirection.East:
                diveDirection = _playerTrs.position + new Vector3(_catchDistance, 0, 0);;
                break;
        }
        sequence.Append(_playerTrs.DOMove(diveDirection, _catchEndTime));
        sequence.AppendInterval(_intervalTime);
        sequence.AppendCallback(() => _isDiving = false);
    }

}
public enum CharacterDirection
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
    North = 1 << 1,
    South = 1 << 2,
    East = 1 << 3,
    West = 1 << 4,
    //ON → CanMoveDirection |= Enum.North
    //Off →CanMoveDirection &= ~Enum.North
}