using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// URP に矩形領域限定Bloomを追加するための Renderer Feature（Render Graph API 版）
public class RegionalBloomFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        // パイプラインのどのタイミングで実行するか
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        // Bloomシェーダー（インスペクターで指定）
        public Shader shader;

        // Bloom適用領域（UV座標: 0〜1）
        [Header("Region (UV Coordinates)")]
        public Vector4 regionMin = new Vector4(0.3f, 0.3f, 0, 0);
        public Vector4 regionMax = new Vector4(0.7f, 0.7f, 0, 0);

        // 輝度しきい値：これより明るいピクセルだけBloomする
        [Header("Bloom Settings")]
        [Range(0, 2)] public float threshold = 0.8f;

        // Bloom の強さ
        [Range(0, 5)] public float intensity = 1.0f;

        // ぼかしのサンプル距離
        [Range(0, 10)] public float blurSize = 3.0f;
        
        // 時間による明滅
        public float flickering = 0.0f;
        
        // 有効にするポジションの配列
        public Vector4[] validPosArray;
    }

    public Settings settings = new Settings();
    private Material material;
    private RegionalBloomPass pass;

    // Feature 初期化時にマテリアルとパスを生成
    public override void Create()
    {
        if (settings.shader == null)
            return;

        material = new Material(settings.shader);
        pass = new RegionalBloomPass(material, settings);
        pass.renderPassEvent = settings.renderPassEvent;
    }

    // 毎フレーム呼ばれ、レンダーパスをキューに登録する
    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (pass == null || material == null)
            return;

        // Game カメラのみに適用（Scene ビューやプレビューは除外）
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(pass);
        }
    }

    // マテリアルの後始末
    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }
}