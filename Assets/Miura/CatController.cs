using System.Collections.Generic;
using UnityEngine;

public class CatController : MonoBehaviour
{
    [Header("怪盗であるかどうか"), SerializeField]
    bool _isPhantom = false;
    public bool isPhantom => _isPhantom;
    CatState _catState = CatState.None;
    CharacterDirection _characterDirection = CharacterDirection.None;
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
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
                ChangeDirection();
                break;
        }
        // 周囲の情報を取得
        _isCanWalkTilesDict = SearchAroundTiles();
        // if () // 周囲の遮蔽物の数が減少したら = CatState.DirectionJudge
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
    void ChangeDirection()
    {
        // ランダムに方向転換
        
    }
    Dictionary<Vector2Int, bool> SearchAroundTiles() //IsoPosをそのままとっても良い
    {
        Vector2Int pos = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        int directionCount = 4;
        Dictionary<Vector2Int, bool> isCanWalkTilesDict = InGameObjectContainer.Instance.IsCanWalkTilesDict;
        for (int i = 0; i < directionCount; i++)
        {
            switch (i)
            {
                case 0: //左上
                    pos += new Vector2Int(-1, 1);
                    break;
                case 1: //右下
                    pos += new Vector2Int(1, -1);
                    break;
                case 2: //左下
                    pos += new Vector2Int(-1, -1);
                    break;
                case 3: //右上
                    pos += new Vector2Int(1, 1);
                    break;
            }
            bool isCanWalk = isCanWalkTilesDict[pos];
            isCanWalkTilesDict.Add(pos, isCanWalk);
        }
        Debug.Log(isCanWalkTilesDict.Count == 4? "Success_isCanWalkTilesDict" : "Fail_isCanWalkTilesDict");
        return isCanWalkTilesDict;
    }
}
public enum CatState
{
    None,
    Walking,
    DirectionJudge,
    Idle,
}
