using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// 矩形領域限定Bloomの描画処理を行うレンダーパス（Render Graph API 版）
public class RegionalBloomPass : ScriptableRenderPass
{
    // シェーダープロパティのID（文字列比較を避けて高速化）
    private static readonly int regionMinId = Shader.PropertyToID("_RegionMin");
    private static readonly int regionMaxId = Shader.PropertyToID("_RegionMax");
    private static readonly int thresholdId = Shader.PropertyToID("_Threshold");
    private static readonly int intensityId = Shader.PropertyToID("_Intensity");
    private static readonly int blurSizeId = Shader.PropertyToID("_BlurSize");
    private static readonly int bloomTexId = Shader.PropertyToID("_BloomTex");
    //
    private static readonly int validPosArrayId = Shader.PropertyToID("_ValidPosArray");
    private static readonly int flickeringId = Shader.PropertyToID("_Flickering");

    // 一時テクスチャの名前
    private const string k_BrightTexName = "_RegionalBloom_BrightTex";
    private const string k_BlurTexName = "_RegionalBloom_BlurTex";
    private const string k_CompositeTempName = "_RegionalBloom_CompositeTmp";

    // パス名
    private const string k_ExtractPassName = "RegionalBloom_Extract";
    private const string k_Blur1PassName = "RegionalBloom_Blur1";
    private const string k_Blur2PassName = "RegionalBloom_Blur2";
    private const string k_CompositePassName = "RegionalBloom_Composite";
    private const string k_CopyBackPassName = "RegionalBloom_CopyBack";

    private Material material;
    private RegionalBloomFeature.Settings settings;

    public RegionalBloomPass(Material material, RegionalBloomFeature.Settings settings)
    {
        this.material = material;
        this.settings = settings;
    }

    // マテリアルにパラメータを反映する
    private void UpdateMaterialProperties()
    {
        if (material == null) return;
        
        material.SetVector(regionMinId, settings.regionMin);
        material.SetVector(regionMaxId, settings.regionMax);
        material.SetFloat(thresholdId, settings.threshold);
        material.SetFloat(intensityId, settings.intensity);
        material.SetFloat(blurSizeId, settings.blurSize);
        material.SetFloat(flickeringId, settings.flickering);
        // 配列が null または空なら渡さない
        if (settings.validPosArray != null && settings.validPosArray.Length > 0)
        {
            material.SetVectorArray(validPosArrayId, settings.validPosArray);
            material.SetInt("_ValidPosCount", settings.validPosArray.Length);
        }
        else
        {
            material.SetInt("_ValidPosCount", 0);
        }
        // material.SetVectorArray(validPosArrayId, settings.validPosArray);
        // material.SetInt("_ValidPosCount", settings.validPosArray.Length);
    }

    // 毎フレーム呼ばれる：Render Graph にパスを登録する
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // URP のリソースデータを取得
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        // バックバッファへの直接描画は避ける
        if (resourceData.isActiveTargetBackBuffer)
            return;

        // カメラのカラーテクスチャを取得
        TextureHandle srcCamColor = resourceData.activeColorTexture;
        if (!srcCamColor.IsValid())
            return;

        // マテリアルのパラメータを更新
        UpdateMaterialProperties();

        // --- 一時テクスチャの作成 ---

        // 輝度抽出用（半分解像度で軽量化 & ぼかし効果を強める）
        var halfDesc = srcCamColor.GetDescriptor(renderGraph);
        halfDesc.width /= 2;
        halfDesc.height /= 2;
        halfDesc.depthBufferBits = 0;
        halfDesc.name = k_BrightTexName;
        TextureHandle brightTex = renderGraph.CreateTexture(halfDesc);

        // ぼかし用（同じ半分解像度）
        var blurDesc = halfDesc;
        blurDesc.name = k_BlurTexName;
        TextureHandle blurTex = renderGraph.CreateTexture(blurDesc);

        // 合成の作業用（フル解像度）
        var fullDesc = srcCamColor.GetDescriptor(renderGraph);
        fullDesc.depthBufferBits = 0;
        fullDesc.name = k_CompositeTempName;
        TextureHandle compositeTmp = renderGraph.CreateTexture(fullDesc);

        // --- Pass 0: 輝度抽出 ---
        // カメラ画面 → brightTex へ、シェーダーPass 0 で描画
        RenderGraphUtils.BlitMaterialParameters extractParams =
            new(srcCamColor, brightTex, material, 0);
        renderGraph.AddBlitPass(extractParams, k_ExtractPassName);

        // --- Pass 1: ぼかし（2回かけてより滑らかに）---
        // brightTex → blurTex
        RenderGraphUtils.BlitMaterialParameters blur1Params =
            new(brightTex, blurTex, material, 1);
        renderGraph.AddBlitPass(blur1Params, k_Blur1PassName);

        // blurTex → brightTex（結果を brightTex に戻す）
        RenderGraphUtils.BlitMaterialParameters blur2Params =
            new(blurTex, brightTex, material, 1);
        renderGraph.AddBlitPass(blur2Params, k_Blur2PassName);

        // --- Pass 2: 合成 ---
        // _BloomTex にぼかし済みテクスチャをセット
        material.SetTexture(bloomTexId, null); // Render Graph では直接セットできないため
        // 合成パスはカスタム実装が必要（_BloomTex を手動バインドするため）
        // AddRasterRenderPass で実装
        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
            k_CompositePassName, out var passData))
        {
            passData.srcCamColor = srcCamColor;
            passData.bloomTex = brightTex;
            passData.material = material;
            passData.intensityId = intensityId;
            passData.bloomTexId = bloomTexId;

            // 入力テクスチャとして宣言
            builder.UseTexture(srcCamColor);
            builder.UseTexture(brightTex);

            // 出力先として compositeTmp を設定
            builder.SetRenderAttachment(compositeTmp, 0);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
            {
                // _BloomTex をマテリアルにバインド
                data.material.SetTexture(data.bloomTexId, data.bloomTex);
                // Blitter API でカメラカラーを読み取り、compositeTmp に合成結果を書き込む
                Blitter.BlitTexture(context.cmd, data.srcCamColor,
                    new Vector4(1, 1, 0, 0), data.material, 2);
            });
        }

        // --- 合成結果をカメラカラーに書き戻す ---
        renderGraph.AddCopyPass(compositeTmp, srcCamColor, passName: k_CopyBackPassName);
    }

    // 合成パスで使うデータ構造
    private class CompositePassData
    {
        public TextureHandle srcCamColor;
        public TextureHandle bloomTex;
        public Material material;
        public int intensityId;
        public int bloomTexId;
    }
}
