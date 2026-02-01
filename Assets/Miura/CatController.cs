using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CatController : MonoBehaviour
{
    [Header("怪盗であるかどうか"), SerializeField]
    bool _isPhantom = false;
    public bool IsPhantom => _isPhantom;
    private CatState _catState = CatState.None;
    public CatState CatState{get => _catState;set{if (_catState != value){Debug.Log($"CatState changed: {_catState} → {value}");_catState = value;}}}
    private CharacterDirection _characterDirection = CharacterDirection.None;
    public CharacterDirection CharacterDirection{get => _characterDirection;set{if (_characterDirection != value){Debug.Log($"CharacterDirection changed: {_characterDirection} → {value}");_characterDirection = value;}}}
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
    CanMoveDirection _canMoveDirection = CanMoveDirection.None;
    // パラメーター関連
    float _moveSpeed = 0.001f;
    
#if UNITY_EDITOR
    void Start()
    {  
        _catState = CatState.Walking;
        _characterDirection = CharacterDirection.North;
    }
#endif
    void Update()
    {
        // ステートマシン
        switch (_catState)
        {
            case CatState.None:
                break;
            case CatState.Walking:
                Move();
                break;
            case CatState.DirectionJudge:
                _characterDirection = ChangeDirection(_canMoveDirection);
                _catState = CatState.Walking;
                break;
        }
        Dictionary<Vector2Int, bool> isCanWalkTilesDict = SearchAroundTiles();
        int isCanWalkTileCount = isCanWalkTilesDict.Values.Count(v => v), beforeIsCanWalkTileCount = _isCanWalkTilesDict.Values.Count(v => v);
        if (beforeIsCanWalkTileCount != isCanWalkTileCount) // 歩ける場所の数が違うのなら  
        {
            _canMoveDirection = UpdateCanMoveDirection(isCanWalkTilesDict); // CanMoveDirectionを変える 
            if (beforeIsCanWalkTileCount < isCanWalkTileCount) // 歩ける場所が増えたのなら
            {
                _catState = CatState.DirectionJudge; // = CatState.DirectionJudge
            }
        }
        // 更新処理
        _isCanWalkTilesDict = isCanWalkTilesDict;
    }
    void Move()
    {
        // 多機能ステートマシン
        switch (_characterDirection)
        {
            case CharacterDirection.North: //左上
                transform.Translate(new Vector2(-1, 1) * _moveSpeed, Space.World);
                break;
            case CharacterDirection.South: //右下
                transform.Translate(new Vector2(1, -1) * _moveSpeed, Space.World);
                break;
            case CharacterDirection.West: //左下
                transform.Translate(new Vector2(-1, -1) * _moveSpeed, Space.World);
                break;
            case CharacterDirection.East: //右上
                transform.Translate(new Vector2(1, 1)  * _moveSpeed, Space.World);
                break;
        }
    }
    CharacterDirection ChangeDirection(CanMoveDirection canMoveDirection)
    {
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
                    posInt += new Vector2Int(-1, 1);
                    break;
                case 1: //右下
                    posInt += new Vector2Int(1, -1);
                    break;
                case 2: //左下
                    posInt += new Vector2Int(-1, -1);
                    break;
                case 3: //右上
                    posInt += new Vector2Int(1, 1);
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
        Vector2Int posIntNorth = new Vector2Int(x - 1, y + 1), posIntSouth = new Vector2Int(x + 1, y - 1),
                posIntEast = new Vector2Int(x - 1, y - 1), posIntWest = new Vector2Int(x + 1, y + 1);
        int directionCount = 4;
        for (int i = 0; i < directionCount; i++)
        {
            switch (i)
            {
                case 0: //左上
                    canMoveDirection = isCanWalkTilesDict[posIntNorth] ? canMoveDirection | CanMoveDirection.North : canMoveDirection & ~CanMoveDirection.North;
                    break;
                case 1: //右下
                    canMoveDirection = isCanWalkTilesDict[posIntSouth] ? canMoveDirection | CanMoveDirection.South : canMoveDirection & ~CanMoveDirection.South;
                    break;
                case 2: //左下
                    canMoveDirection = isCanWalkTilesDict[posIntWest] ? canMoveDirection | CanMoveDirection.West : canMoveDirection & ~CanMoveDirection.West;
                    break;
                case 3: //右上
                    canMoveDirection = isCanWalkTilesDict[posIntEast] ? canMoveDirection | CanMoveDirection.East : canMoveDirection & ~CanMoveDirection.East;
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
    DirectionJudge,
    Idle,
}
