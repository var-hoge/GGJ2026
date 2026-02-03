using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using IsoTools;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
[RequireComponent(typeof(CircleCollider2D))]
public class CatController : MonoBehaviour
{
    [Header("怪盗であるかどうか"), SerializeField]
    bool _isPhantom = false;
    public bool IsPhantom => _isPhantom;
    IsoObject _isoObject;
    CatState _catState = CatState.None;
    CatState _CatState{get => _catState;set{if (_catState != value){
        // Debug.Log($"CatState changed: {_catState} → {value}");
            _catState = value;}}}
    Direction _characterDirection = Direction.None;
    Direction _CharacterDirection{get => _characterDirection;set{if (_characterDirection != value){
            // Debug.Log($"Direction changed: {_characterDirection} → {value}");
            _characterDirection = value;}}}
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
    CanMoveDirection _canMoveDirection = CanMoveDirection.None;
    // パラメーター関連
    [SerializeField, Range(0.001f, 10)]
    float _moveSpeed = 0.001f;
    float _moveX = 1f;
    float _moveY = 0.5f;
    Vector2 _beforeWorldPos = Vector2.zero;
    float _worldPosXDistance = 0.65f;
    float _worldPosYDistance = 0.3f;
    float _tileDistanceX = 0.65f;
    float _tileDistanceY = 0.3f;
#if UNITY_EDITOR
    void Start()
    {  
        _catState = CatState.Walking;
        _characterDirection = Direction.North;
    }
#endif
    void Awake()
    {
        _isoObject = GetComponent<IsoObject>();
        Vector2Int startTilePos = WorldToTilePosition(transform.position);
        _isoObject.position = new(startTilePos.x, startTilePos.y);
        _beforeWorldPos = TileToWorldPosition(startTilePos);
        // Debug.Log($"world:{_beforeWorldPos} tile:{startTilePos}");
        GetComponent<CircleCollider2D>().isTrigger = true;
    }
    void Update()
    {
        // ステートマシン
        switch (_CatState)
        {
            case CatState.None:
                break;
            case CatState.Walking:
                Move();
                // 違うマスに進んでいたら
                Vector2 pos = transform.position;
                if ((_beforeWorldPos.x - _worldPosXDistance > pos.x || _beforeWorldPos.x + _worldPosXDistance < pos.x) &&
                    (_beforeWorldPos.y - _worldPosYDistance > pos.y || _beforeWorldPos.y + _worldPosYDistance < pos.y))
                {
                    Vector2Int updateTilePos = WorldToTilePosition(pos);
                    _isoObject.position = new(updateTilePos.x, updateTilePos.y);
                    _beforeWorldPos = TileToWorldPosition(updateTilePos); //値を整形
                    _CatState = CatState.DataUpdate;
                }
                break;
            case CatState.DataUpdate: //更新処理
                Dictionary<Vector2Int, bool> isCanWalkTilesDict = SearchAroundTiles();
                int isCanWalkTileCount = isCanWalkTilesDict.Values.Count(v => v), beforeIsCanWalkTileCount = _isCanWalkTilesDict.Values.Count(v => v);
                if (beforeIsCanWalkTileCount != isCanWalkTileCount) // 歩ける場所の数が違うのなら
                {
                    _canMoveDirection = UpdateCanMoveDirection(isCanWalkTilesDict); // CanMoveDirectionを変える 
                    // if (beforeIsCanWalkTileCount < isCanWalkTileCount) // 歩ける場所が増えたのなら
                    // {
                        // _CatState = CatState.DirectionJudge; // キャラクターの移動方向をランダムに決定する
                        _CharacterDirection = ChangeDirection(_canMoveDirection);
                        // Debug.Log(_canMoveDirection);
                    // }
                }
                _isCanWalkTilesDict = isCanWalkTilesDict;
                _CatState = CatState.Walking;
                break;
        }
    }
    void Move()
    {
        switch (_CharacterDirection)
        {
            case Direction.North: //左上
                transform.Translate(new Vector2(-_moveX, _moveY) * _moveSpeed, Space.World);
                break;
            case Direction.South: //右下
                transform.Translate(new Vector2(_moveX, -_moveY) * _moveSpeed, Space.World);
                break;
            case Direction.West: //左下
                transform.Translate(new Vector2(-_moveX, -_moveY) * _moveSpeed, Space.World);
                break;
            case Direction.East: //右上
                transform.Translate(new Vector2(_moveX, _moveY)  * _moveSpeed, Space.World);
                break;
        }
    }
    Direction ChangeDirection(CanMoveDirection canMoveDirection)
    {
        // ランダムに方向転換
        List<Direction> canMoveDirectionList = new List<Direction>();
        if ((canMoveDirection & CanMoveDirection.North) != 0) canMoveDirectionList.Add(Direction.North);
        if ((canMoveDirection & CanMoveDirection.South) != 0) canMoveDirectionList.Add(Direction.South);
        if ((canMoveDirection & CanMoveDirection.East) != 0) canMoveDirectionList.Add(Direction.East);
        if ((canMoveDirection & CanMoveDirection.West) != 0) canMoveDirectionList.Add(Direction.West);
        if (canMoveDirectionList.Count == 0)
        {
            Debug.LogWarning("通過できる通路がありません"); return Direction.None;
        }
        return canMoveDirectionList[Random.Range(0, canMoveDirectionList.Count)];
    }
    /// <summary>
    /// キャラクターの周囲にあるタイルをすべて取得する
    /// </summary>
    /// <returns></returns>
    Dictionary<Vector2Int, bool> SearchAroundTiles() //IsoPosをそのままとっても良い
    {
        int directionCount = 4;
        Dictionary<Vector2Int, bool> isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
        for (int i = 0; i < directionCount; i++)
        {
            Vector2Int posInt = WorldToTilePosition(transform.position);
            switch (i)
            {
                case 0: //左上
                    posInt += new Vector2Int(0, 1);
                    break;
                case 1: //右下
                    posInt += new Vector2Int(0, -1);
                    break;
                case 2: //左下
                    posInt += new Vector2Int(-1, 0);
                    break;
                case 3: //右上
                    posInt += new Vector2Int(1, 0);
                    break;
            }
            bool isCanWalk = InGameObjectContainer.Instance.IsCanWalkTilesDict[posInt];
            isCanWalkTilesDict.Add(posInt, isCanWalk);
        }
        return isCanWalkTilesDict;
    }
    /// <summary> _canMoveDirectionの更新 </summary>
    /// <param name="isCanWalkTilesDict"></param>
    /// <returns></returns>
    CanMoveDirection UpdateCanMoveDirection(Dictionary<Vector2Int, bool> isCanWalkTilesDict)
    {
        CanMoveDirection canMoveDirection = CanMoveDirection.None;
        Vector2Int posInt = WorldToTilePosition(transform.position);
        int x = posInt.x, y = posInt.y;
        Vector2Int posIntNorth = new (x, y + 1), posIntSouth = new (x, y - 1), posIntWest = new (x - 1, y), posIntEast = new (x + 1, y);
        int directionCount = 4;
        for (int i = 0; i < directionCount; i++)
        {
            switch (i)
            {
                case 0: //左上
                    canMoveDirection = isCanWalkTilesDict[posIntNorth]? canMoveDirection | CanMoveDirection.North : canMoveDirection & ~CanMoveDirection.North;
                    break;
                case 1: //右下
                    canMoveDirection = isCanWalkTilesDict[posIntSouth]? canMoveDirection | CanMoveDirection.South : canMoveDirection & ~CanMoveDirection.South;
                    break;
                case 2: //左下
                    canMoveDirection = isCanWalkTilesDict[posIntWest]? canMoveDirection | CanMoveDirection.West : canMoveDirection & ~CanMoveDirection.West;
                    break;
                case 3: //右上
                    canMoveDirection = isCanWalkTilesDict[posIntEast]? canMoveDirection | CanMoveDirection.East : canMoveDirection & ~CanMoveDirection.East;
                    break;
            }
        }
        return canMoveDirection;
    }
    Vector2Int WorldToTilePosition(Vector3 pos) => new (Mathf.RoundToInt(pos.x / _tileDistanceX), Mathf.RoundToInt(pos.y / _tileDistanceY));
    /// <summary> タイル座標の中心点を取得する </summary> param name="pos"></param> <returns></returns>
    Vector3 TileToWorldPosition(Vector2Int pos) => new (pos.x * _tileDistanceX, pos.y * _tileDistanceY);
}
public enum CatState
{
    None,
    Walking,
    DataUpdate,
}