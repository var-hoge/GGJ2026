using UnityEngine;
using System.Collections.Generic;
using System;
using IsoTools;
public class InGameObjectContainer : MonoBehaviour
{
    [SerializeField] IsoObject[] _isoArray;
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
    public Dictionary<Vector2Int, bool> IsCanWalkTilesDict => _isCanWalkTilesDict;
    public static InGameObjectContainer Instance;
    HashSet<Vector2Int> test = new HashSet<Vector2Int>();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        foreach (var isoObj in _isoArray) //非アクティブまで全取得)
        {
            // if (new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)) == new Vector2(-2 ,1) ||
            //     new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)) == new Vector2(0 ,-1) ||
            //     new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)) == new Vector2(-2 ,-1) ||
            //     new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)) == new Vector2(0 ,1))
            //     Debug.Log(isoObj.name);
            _isCanWalkTilesDict
                .Add(new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)),
                    isoObj.IsCanWalk);
        }
    }
}
