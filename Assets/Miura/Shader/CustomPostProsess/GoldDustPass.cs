using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// 金粉発光エフェクトの描画処理を行うレンダーパス（Render Graph API 版）
public class GoldDustPass : ScriptableRenderPass
{
    // シェーダープロパティのID
    private static readonly int goldDensityId = Shader.PropertyToID("_GoldDensity");
    private static readonly int goldIntensityId = Shader.PropertyToID("_GoldIntensity");
    private static readonly int goldBlurSizeId = Shader.PropertyToID("_GoldBlurSize");
    private static readonly int goldBloomTexId = Shader.PropertyToID("_GoldBloomTex");

    // パス名
    private const string k_ExtractPassName = "GoldDust_Extract";
    private const string k_Blur1PassName = "GoldDust_Blur1";
    private const string k_Blur2PassName = "GoldDust_Blur2";
    private const string k_CompositePassName = "GoldDust_Composite";
    private const string k_CopyBackPassName = "GoldDust_CopyBack";

    private Material material;
    private GoldDustFeature.Settings settings;

    public GoldDustPass(Material material, GoldDustFeature.Settings settings)
    {
        this.material = material;
        this.settings = settings;
    }

    // マテリアルにパラメータを反映する
    private void UpdateMaterialProperties()
    {
        if (material == null) return;

        material.SetFloat(goldDensityId, settings.goldDensity);
        material.SetFloat(goldIntensityId, settings.goldIntensity);
        material.SetFloat(goldBlurSizeId, settings.goldBlurSize);
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

        // 金粉抽出用（半分解像度で軽量化 & ぼかし効果を強める）
        var halfDesc = srcCamColor.GetDescriptor(renderGraph);
        halfDesc.width /= 2;
        halfDesc.height /= 2;
        halfDesc.depthBufferBits = 0;
        halfDesc.name = "_GoldDust_ExtractTex";
        TextureHandle extractTex = renderGraph.CreateTexture(halfDesc);

        // ぼかし用（同じ半分解像度）
        var blurDesc = halfDesc;
        blurDesc.name = "_GoldDust_BlurTex";
        TextureHandle blurTex = renderGraph.CreateTexture(blurDesc);

        // 合成の作業用（フル解像度）
        var fullDesc = srcCamColor.GetDescriptor(renderGraph);
        fullDesc.depthBufferBits = 0;
        fullDesc.name = "_GoldDust_CompositeTmp";
        TextureHandle compositeTmp = renderGraph.CreateTexture(fullDesc);

        // --- Pass 0: 金粉抽出 ---
        // カメラ画面 → extractTex へ、金粉だけ描く
        RenderGraphUtils.BlitMaterialParameters extractParams =
            new(srcCamColor, extractTex, material, 0);
        renderGraph.AddBlitPass(extractParams, k_ExtractPassName);

        // --- Pass 1: ぼかし（2回かけてより滑らかに）---
        // extractTex → blurTex
        RenderGraphUtils.BlitMaterialParameters blur1Params =
            new(extractTex, blurTex, material, 1);
        renderGraph.AddBlitPass(blur1Params, k_Blur1PassName);

        // blurTex → extractTex（結果を extractTex に戻す）
        RenderGraphUtils.BlitMaterialParameters blur2Params =
            new(blurTex, extractTex, material, 1);
        renderGraph.AddBlitPass(blur2Params, k_Blur2PassName);

        // --- Pass 2: 合成 ---
        // _GoldBloomTex にぼかし済みテクスチャをセットし、
        // 元の画面と加算合成する
        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
            k_CompositePassName, out var passData))
        {
            passData.srcCamColor = srcCamColor;
            passData.goldBloomTex = extractTex;
            passData.material = material;
            passData.goldBloomTexId = goldBloomTexId;

            // 入力テクスチャとして宣言
            builder.UseTexture(srcCamColor);
            builder.UseTexture(extractTex);

            // 出力先として compositeTmp を設定
            builder.SetRenderAttachment(compositeTmp, 0);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
            {
                // _GoldBloomTex をマテリアルにバインド
                data.material.SetTexture(data.goldBloomTexId, data.goldBloomTex);
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
        public TextureHandle goldBloomTex;
        public Material material;
        public int goldBloomTexId;
    }
}