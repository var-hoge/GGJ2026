using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary> 各ImageShaderに他のコンポーネントのプロパティを渡すクラス </summary>
[ExecuteAlways]
public class BridgeToImageShaders : MonoBehaviour
{
    Image _image;
    Material _imageMaterial;
    void OnEnable()
    {
        _image = GetComponent<Image>();
        _imageMaterial = _image.material;
        _imageMaterial.SetFloat("_Width", Screen.width);
        _imageMaterial.SetFloat("_Height", Screen.height);
        _image.rectTransform.sizeDelta = new (Screen.width, Screen.height);
    }
}
