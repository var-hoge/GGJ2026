using UnityEngine;

public class TestTimeDebug : MonoBehaviour
{
    void Update()
    {
        // _Time の中身
        // x = t/20, y = t, z = t*2, w = t*3
        float t = Time.timeSinceLevelLoad;
        Debug.Log($"_Time.y={t}, frac={t % 1f}");
    }
}
