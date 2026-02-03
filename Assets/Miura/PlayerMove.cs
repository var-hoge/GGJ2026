using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Linq;
using DG.Tweening;
using IsoTools;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField, Range(0.001f, 0.01f)]
    float _moveSpeed = 0.01f; //
    Transform _playerTrs;
    Direction _characterDirection = Direction.None;
    bool _isDiving = false;
    float _intervalTime = 0.5f;
    float _catchEndTime = 1.2f;
    float _catchDistanceX = 1f;
    float _catchDistanceY = 0.5f;
    float _moveX = 1f;
    float _moveY = 0.5f;
    GameObject _activeFlashLight;
    void Awake()
    {
        _playerTrs = transform;
        _characterDirection = Direction.East;
        GetComponent<CircleCollider2D>().isTrigger = true;
        GetComponent<CircleCollider2D>().enabled = false;
        GameObject nextLight = InGameObjectContainer.Instance.PlayerFlashLightArray.FirstOrDefault(obj =>  Variables.Object(obj).Get<Direction>("Direction") == _characterDirection);
        nextLight.SetActive(true);
        _activeFlashLight = nextLight;
    }
    
    void Update()
    {
        if (!_isDiving)
        {
            // 移動制御
            Vector2 moveDirection = Vector2.zero;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                _characterDirection = Direction.North;
                moveDirection = new Vector2(-_moveX, _moveY);
            }
            else if ( Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                _characterDirection = Direction.South;
                moveDirection = new Vector2(_moveX, -_moveY);
            }
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                _characterDirection = Direction.West;
                moveDirection = new Vector2(-_moveX, -_moveY);
            }
            else if (Input.GetKey(KeyCode.RightArrow) ||  Input.GetKey(KeyCode.D))
            {
                _characterDirection = Direction.East;
                moveDirection = new Vector2(_moveX, _moveY);
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
                case Direction.North:
                    UpdateFlashLight(Direction.North);
                    break;
                case Direction.South:
                    UpdateFlashLight(Direction.South);
                    break;
                case Direction.West:
                    UpdateFlashLight(Direction.West);
                    break;
                case Direction.East:
                    UpdateFlashLight(Direction.East);
                    break;
            }
        }
    }
    void CatchDive()
    {
        _isDiving = true;
        GetComponent<CircleCollider2D>().enabled = true; //コライダーオン
        Vector3 diveDirection = Vector2.zero;
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        // 向いている方向にイージングを付けて移動する
        switch (_characterDirection)
        {
            case Direction.North:
                diveDirection = _playerTrs.position + new Vector3(-_catchDistanceX, _catchDistanceY, 0);
                break;
            case Direction.South:
                diveDirection = _playerTrs.position + new Vector3(_catchDistanceX, -_catchDistanceY, 0);
                break;
            case Direction.West:
                diveDirection = _playerTrs.position + new Vector3(-_catchDistanceX, -_catchDistanceY, 0);
                break;
            case Direction.East:
                diveDirection = _playerTrs.position + new Vector3(_catchDistanceX, _catchDistanceY, 0);
                break;
        }
        sequence.Append(_playerTrs.DOMove(diveDirection, _catchEndTime));
        sequence.AppendInterval(_intervalTime);
        sequence.AppendCallback(() => _isDiving = false);
        sequence.AppendCallback(() => GetComponent<CircleCollider2D>().enabled = false);
        sequence.Play();
    }

    void UpdateFlashLight(Direction direction)
    {
        GameObject nextLight = InGameObjectContainer.Instance.PlayerFlashLightArray.FirstOrDefault(obj =>  Variables.Object(obj).Get<Direction>("Direction") == direction);
        if (!nextLight.activeSelf)
        {
            _activeFlashLight.SetActive(false);
            nextLight.SetActive(true);
            _activeFlashLight = nextLight;
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        CatController catController = other.gameObject.GetComponent<CatController>();
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
public enum Direction
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
