using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class CatController : MonoBehaviour
{
    [Header("怪盗であるかどうか"), SerializeField]
    bool _isPhantom = false;
    public bool IsPhantom => _isPhantom;
    CatState _catState = CatState.None;
    CatState _CatState{get => _catState;set{if (_catState != value){
        // Debug.Log($"CatState changed: {_catState} → {value}");
            _catState = value;}}}
    CharacterDirection _characterDirection = CharacterDirection.None;
    CharacterDirection _CharacterDirection{get => _characterDirection;set{if (_characterDirection != value){
            // Debug.Log($"CharacterDirection changed: {_characterDirection} → {value}");
            _characterDirection = value;}}}
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
    CanMoveDirection _canMoveDirection = CanMoveDirection.None;
    // パラメーター関連
    bool _isCnaChangeState = false;
    float _moveSpeed = 0.001f;
    float _moveX = 1f;
    float _moveY = 0.5f;
    Vector2 _beforeWorldPos = Vector2.zero;
    float _worldPosXDistance = 0.65f;
    float _worldPosYDistance = 0.3f;
#if UNITY_EDITOR
    void Start()
    {  
        _catState = CatState.Walking;
        _characterDirection = CharacterDirection.North;
    }
#endif
    void Awake()
    {
        _beforeWorldPos = transform.position;
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
                if (_beforeWorldPos.x - _worldPosXDistance > pos.x ||_beforeWorldPos.x + _worldPosXDistance < pos.x ||
                    _beforeWorldPos.y - _worldPosXDistance > pos.y ||_beforeWorldPos.y + _worldPosYDistance < pos.y)
                {
                    // Debug.Log(transform.position);
                    float updateX = _beforeWorldPos.x + _worldPosXDistance < pos.x? _beforeWorldPos.x + _worldPosXDistance : _beforeWorldPos.x - _worldPosXDistance;
                    float updateY = _beforeWorldPos.y + _worldPosYDistance < pos.y? _beforeWorldPos.y + _worldPosYDistance : _beforeWorldPos.y - _worldPosYDistance;
                    _beforeWorldPos = new Vector2(updateX, updateY);  //値を整形
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
        // 多機能ステートマシン
        switch (_CharacterDirection)
        {
            case CharacterDirection.North: //左上
                transform.Translate(new Vector2(-_moveX, _moveY) * _moveSpeed, Space.World);
                break;
            case CharacterDirection.South: //右下
                transform.Translate(new Vector2(_moveX, -_moveY) * _moveSpeed, Space.World);
                break;
            case CharacterDirection.West: //左下
                transform.Translate(new Vector2(-_moveX, -_moveY) * _moveSpeed, Space.World);
                break;
            case CharacterDirection.East: //右上
                transform.Translate(new Vector2(_moveX, _moveY)  * _moveSpeed, Space.World);
                break;
        }
    }
    CharacterDirection ChangeDirection(CanMoveDirection canMoveDirection)
    {
        Debug.Log(canMoveDirection);
        // ランダムに方向転換
        List<CharacterDirection> canMoveDirectionList = new List<CharacterDirection>();
        if ((canMoveDirection & CanMoveDirection.North) != 0) canMoveDirectionList.Add(CharacterDirection.North);
        if ((canMoveDirection & CanMoveDirection.South) != 0) canMoveDirectionList.Add(CharacterDirection.South);
        if ((canMoveDirection & CanMoveDirection.East) != 0) canMoveDirectionList.Add(CharacterDirection.East);
        if ((canMoveDirection & CanMoveDirection.West) != 0) canMoveDirectionList.Add(CharacterDirection.West);
        if (canMoveDirectionList.Count == 0)
        {
            Debug.LogWarning("通過できる通路がありません"); return CharacterDirection.None;
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
            Vector2Int posInt = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
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
        int x = Mathf.RoundToInt(transform.position.x), y = Mathf.RoundToInt(transform.position.y);
        Vector2Int posIntNorth = new Vector2Int(x, y + 1), posIntSouth = new Vector2Int(x, y - 1),
                posIntWest = new Vector2Int(x - 1, y), posIntEast = new Vector2Int(x + 1, y);
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
}
public enum CatState
{
    None,
    Walking,
    DataUpdate,
}
