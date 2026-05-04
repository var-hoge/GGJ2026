using IsoTools;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        foreach (var obj in GetComponentsInChildren<IsoObject>())
        {
            Debug.Log($"位置設定 : {obj.gameObject.name}");
            obj.position = obj.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
