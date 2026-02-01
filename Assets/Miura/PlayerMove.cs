using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.Rendering.UI;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerMove : MonoBehaviour
{
    //まずはプレイヤーのトランスフォームを取得する
    Transform _playerTrs;
    CharacterDirection _characterDirection;
    bool _isDiving = false;
    float _intervalTime = 0.5f;
    float _catchEndTime = 1.2f;
    float _catchDistance = 0.05f;
    float _moveSpeed = 0.001f; //仮で10
    float _moveX = 1f;
    float _moveY = 0.5f;
    void Awake()
    {
        _playerTrs = transform;
        GetComponent<CircleCollider2D>().enabled = false;
        GetComponent<CircleCollider2D>().isTrigger = true;
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
                if (moveDirection.y > 0)
                {
                    moveDirection = new Vector2(-_moveY, _moveY);
                }
                else if (moveDirection.y < 0)
                {
                    moveDirection = new Vector2(_moveY, -_moveY);
                }
            }
            else if (_characterDirection == CharacterDirection.East || _characterDirection == CharacterDirection.West) 
            {
                moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
                if (moveDirection.x > 0) //右上
                {
                    moveDirection = new Vector2(+_moveX, +_moveY);
                }
                else if (moveDirection.x < 0)
                {
                    moveDirection = new Vector2(-_moveX, -_moveY);
                }
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
        GetComponent<CircleCollider2D>().enabled = true; //コライダーオン
        Vector3 diveDirection = Vector2.zero;
        Sequence sequence = DOTween.Sequence();
        // 向いている方向にイージングを付けて移動する
        switch (_characterDirection)
        {
            case CharacterDirection.North:
                diveDirection = _playerTrs.position + new Vector3(-_catchDistance, _catchDistance, 0);
                break;
            case CharacterDirection.South:
                diveDirection = _playerTrs.position + new Vector3(_catchDistance, -_catchDistance, 0);
                break;
            case CharacterDirection.West:
                diveDirection = _playerTrs.position + new Vector3(-_catchDistance, -_catchDistance, 0);
                break;
            case CharacterDirection.East:
                diveDirection = _playerTrs.position + new Vector3(_catchDistance, _catchDistance, 0);
                break;
        }
        sequence.Append(_playerTrs.DOMove(diveDirection, _catchEndTime));
        sequence.AppendInterval(_intervalTime);
        sequence.AppendCallback(() => _isDiving = false);
        sequence.AppendCallback(() => GetComponent<CircleCollider2D>().enabled = false);
        sequence.Play();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        CatController catController = other.GetComponent<CatController>();
        if (catController != null)
        {
            if (catController.IsPhantom)
            {
                GameManager.Instance.MoveToSuccessScene();
            }
            else
            {
                GameManager.Instance.MoveToFailScene();
            }
        }
        else
        {
            Debug.Log("得体の知れないものを捕まえた");
        }
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
[Flags]
public enum CanMoveDirection
{
    None  = 0,     
    North = 1 << 0,
    South = 1 << 1,
    East  = 1 << 2,
    West  = 1 << 3
    //ON → CanMoveDirection |= Enum.North
    //Off →CanMoveDirection &= ~Enum.North
}