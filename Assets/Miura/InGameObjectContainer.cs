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
    Dictionary<Vector2Int, bool> _isCanWalkTilesDict = new Dictionary<Vector2Int, bool>();
    public Dictionary<Vector2Int, bool> IsCanWalkTilesDict => _isCanWalkTilesDict;
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
        foreach (var isoObj in _isoArray) //非アクティブまで全取得
        {
            _isCanWalkTilesDict
                .Add(new Vector2Int(Mathf.RoundToInt(isoObj.position.x), Mathf.RoundToInt(isoObj.position.y)),
                    isoObj.IsCanWalk);
        }
        Array.ForEach(_PlayerFlashLightArray, lightObj => lightObj.SetActive(false));
    }
}
