using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Linq;
using IsoTools;
using IsoTools.Physics;
using IsoTools.Examples.Kenney;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
[RequireComponent(typeof(IsoObject), typeof(IsoBoxCollider), typeof(IsoRigidbody))]
public class PlayerController : MonoBehaviour
{
    IsoObject _isoObject;
    IsoRigidbody _isoRigidbody; //あんまり使いたくない
    Direction _characterDirection = Direction.None;
    GameObject _activeFlashLight;
    CircleCollider2D _circleCollider2D;
    Vector3 _beforeIsoPos = Vector3.zero;
    // パラメータ関連
    bool _isDiving = false;
    [SerializeField, Range(0.001f, 0.01f)]
    float _moveSpeed = 0.01f;
    [SerializeField, Range(0.1f, 1f)]
    float _catchDistance = 0.1f;
    float _catchInterval = 0.5f;
    float _catchEndTime = 1.2f;
    void Awake()
    {
        _isoObject = GetComponent<IsoObject>();
        _isoRigidbody = GetComponent<IsoRigidbody>();
        _circleCollider2D = GetComponent<CircleCollider2D>();
        _beforeIsoPos = _isoObject.position;
        _circleCollider2D.isTrigger = true;
        _circleCollider2D.enabled = false;
        GameObject nextLight = InGameObjectContainer.Instance.PlayerFlashLightArray.FirstOrDefault(obj =>  Variables.Object(obj).Get<Direction>("Direction") == Direction.East);
        nextLight.SetActive(true);
        _activeFlashLight = nextLight;
    }
    
    void Update()
    {
        if (!_isDiving)
        {
            var floorPos = IsoToFloorPosition(_isoObject.position);
            if (floorPos == new Vector2Int(2, 1)) Debug.Log($"ここだ!!{_isoObject.position}");
            
            if (InGameObjectContainer.Instance.CanNotWalkTileArray.Any(dontWalk => dontWalk == floorPos)){
                _isoObject.position = _beforeIsoPos;
            }
            else{
            　// 移動制御
                 Move();}
            // キャッチダイブ
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartCoroutine(CatchDive());
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
        _beforeIsoPos = _isoObject.position;
    }
    void Move()
    {
        Vector3 moveDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            _characterDirection = Direction.North;
            moveDirection.y++;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            _characterDirection = Direction.South;
            moveDirection.y--;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            _characterDirection = Direction.West;
            moveDirection.x--;
        }
        else if (Input.GetKey(KeyCode.RightArrow) ||  Input.GetKey(KeyCode.D))
        {
            _characterDirection = Direction.East;
            moveDirection.x++;
        }
        _isoObject.position += moveDirection * _moveSpeed;
    }

    IEnumerator CatchDive()
    {
        _isDiving = true;
        _circleCollider2D.enabled = true;
        Vector3 startPos = _isoObject.position, diveEndPos = _isoObject.position;
        // 向いている方向にイージングを付けて移動する
        switch (_characterDirection)
        {
            case Direction.North:
                diveEndPos.y += _catchDistance;
                break;
            case Direction.South:
                diveEndPos.y -= _catchDistance;
                break;
            case Direction.West:
                diveEndPos.x -= _catchDistance;
                break;
            case Direction.East:
                diveEndPos.x += _catchDistance;
                break;
        }
        float time = 0f;
        while (time < _catchEndTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _catchEndTime);
            _isoObject.position = Vector3.Lerp(startPos, diveEndPos, t);
            yield return null;
        }
        _isoObject.position = diveEndPos;
        yield return new WaitForSeconds(_catchInterval);
        _circleCollider2D.enabled = false;
        _isDiving = false;
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
    }
    void OnIsoCollisionEnter(IsoCollision iso_collision) {
        if ( iso_collision.gameObject ) {
            var alient = iso_collision.gameObject.GetComponent<AlienBallController>();
            if ( alient ) {
                Destroy(alient.gameObject);
            }
        }
        Debug.Log("Miura");
    }
    Vector2Int IsoToFloorPosition(Vector3 isoPos) => new (Mathf.FloorToInt(isoPos.x), Mathf.FloorToInt(isoPos.y));
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
