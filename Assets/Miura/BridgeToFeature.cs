using UnityEngine;

public class BridgeToFeature : MonoBehaviour
{
    [SerializeField] private RectTransform[] validImageArray;
    [SerializeField] private RegionalBloomFeature bloomFeature;
    [SerializeField] private Camera targetCamera;

    private void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (bloomFeature == null || validImageArray == null || targetCamera == null)
            return;

        bloomFeature.settings.validPosArray = new Vector4[validImageArray.Length];

        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < validImageArray.Length; i++)
        {
            if (validImageArray[i] == null)
            {
                bloomFeature.settings.validPosArray[i] = Vector4.zero;
                continue;
            }

            validImageArray[i].GetWorldCorners(corners);

            Vector2 min = targetCamera.WorldToViewportPoint(corners[0]);
            Vector2 max = targetCamera.WorldToViewportPoint(corners[2]);

            bloomFeature.settings.validPosArray[i] = new Vector4(min.x, min.y, max.x, max.y);
        }
    }
}