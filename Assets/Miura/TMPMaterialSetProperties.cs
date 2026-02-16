using System;
using TMPro;
using UnityEngine;
[ExecuteAlways]
public class TMPMaterialSetProperties : MonoBehaviour
{
    Material _mat;
    RectTransform _rectTransform;
    void OnEnable()
    {
        _mat = GetComponent<TextMeshProUGUI>().material;
        _rectTransform = GetComponent<RectTransform>();
        _mat.SetFloat("_RectWidth", _rectTransform.rect.width);
        _mat.SetFloat("_RectHeight", _rectTransform.rect.height);
    }
}
