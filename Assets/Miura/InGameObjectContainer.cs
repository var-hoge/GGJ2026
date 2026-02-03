using UnityEngine;
using System.Collections.Generic;
using System;
using IsoTools;
[DefaultExecutionOrder(-10)]
public class InGameObjectContainer : MonoBehaviour
{
    [SerializeField] IsoObject[] _isoArray;
    [SerializeField] GameObject[] _PlayerFlashLightArray;
    public GameObject[] PlayerFlashLightArray => _PlayerFlashLightArray;
    Dictionary<Vector3, Vector2Int> _tilesPositonDict = new Dictionary<Vector3, Vector2Int>();
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
    public Dictionary<Vector2Int, bool> IsCanWalkTilesDict => _isCanWalkTilesDict;
    public Dictionary<Vector3, Vector2Int> TilesPositonDict => _tilesPositonDict;
    public static InGameObjectContainer Instance;
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
        _tilesPositonDict = SetTilesPositonDict();
        foreach (var isoObj in _isoArray) //非アクティブまで全取得
        {
            _isCanWalkTilesDict
                .Add(new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)),
                    isoObj.IsCanWalk);
        }
        Array.ForEach(_PlayerFlashLightArray, lightObj => lightObj.SetActive(false));
    }
    Dictionary<Vector3, Vector2Int> SetTilesPositonDict()
    {
        Dictionary<Vector3, Vector2Int> tilesPositonDict = new Dictionary<Vector3, Vector2Int>();
        float xDis = 0.65f, yDis = 0.325f;
        int xMultiplier = default, yMultiplier = default;
        // タイルの数だけ実行
        for (int xInt = -10; xInt < 11; xInt ++)
        {
            xMultiplier = xInt + 10;
            yMultiplier = xInt - 10;
            for (int yInt = -10; yInt < 11; yInt++)
            {
                Debug.Log(new Vector3(xDis * xMultiplier,  yDis * yMultiplier, 0));
                tilesPositonDict.Add(new Vector3(xDis * xMultiplier,  yDis * yMultiplier, 0), new Vector2Int(xInt, yInt));
                xMultiplier--;
                yMultiplier++;
            }
        }
        return tilesPositonDict;
    }
}
