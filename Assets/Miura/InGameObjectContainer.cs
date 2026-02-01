using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using IsoTools;
public class InGameObjectContainer : MonoBehaviour
{
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
        foreach (var isoObj in FindObjectsByType<IsoObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)) //非アクティブまで全取得)
        {
            _isCanWalkTilesDict
                .Add(new Vector2Int((int)isoObj.position.x, (int)isoObj.position.y),
                    isoObj.IsCanWalk);
        }
    }
}
