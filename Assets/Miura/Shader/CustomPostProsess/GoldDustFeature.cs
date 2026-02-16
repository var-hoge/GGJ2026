using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// URP に金粉発光エフェクトを追加するための Renderer Feature
public class GoldDustFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        // パイプラインのどのタイミングで実行するか
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        // 金粉シェーダー（インスペクターで Custom/TestGoldDust を指定）
        public Shader shader;

        // 金粉の密度（値が大きいほどセルが細かく粒が多い）
        [Header("Gold Dust Settings")]
        [Range(100, 2000)] public float goldDensity = 500f;

        // 金粉の明るさ
        [Range(0, 5)] public float goldIntensity = 1.0f;

        // ぼかしの距離（値が大きいほど光が広がる）
        [Range(1, 20)] public float goldBlurSize = 5.0f;
    }

    public Settings settings = new Settings();
    private Material material;
    private GoldDustPass pass;

    // Feature 初期化時にマテリアルとパスを生成
    public override void Create()
    {
        if (settings.shader == null)
            return;

        material = new Material(settings.shader);
        pass = new GoldDustPass(material, settings);
        pass.renderPassEvent = settings.renderPassEvent;
    }

    // 毎フレーム呼ばれ、レンダーパスをキューに登録する
    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (pass == null || material == null)
            return;

        // Game カメラのみに適用
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