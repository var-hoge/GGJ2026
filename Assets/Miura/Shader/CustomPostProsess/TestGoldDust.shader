Shader "Custom/TestGoldDust"
{
    HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // 金粉の密度（値が大きいほどセルが細かく粒が多い）
        float _GoldDensity;
        // 金粉の明るさ
        float _GoldIntensity;
        // ぼかしのサンプル距離
        float _GoldBlurSize;
        // ぼかし済み金粉テクスチャ（合成パスで使用）
        TEXTURE2D(_GoldBloomTex);
        SAMPLER(sampler_GoldBloomTex);

        // 擬似ランダム関数（UV座標から 0〜1 のランダム値を返す）
        float Hash(float2 p)
        {
            float h = dot(p, float2(127.1, 311.7));
            return frac(sin(h) * 43758.5453123);
        }

        // ----------------------------------------------------------
        // Pass 0: 金粉抽出
        // ランダムな位置に金色のピクセルだけを描く（金粉以外は黒）
        // ----------------------------------------------------------
        half4 FragGoldExtract(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            // UVをセルに分割
            float2 cellID = floor(uv * _GoldDensity);

            // セルごとにランダム値を生成
            float rand = Hash(cellID);

            // ランダム値がしきい値以下なら金粉を表示
            float threshold = 0.02;
            float isGold = step(rand, threshold);

            // 金粉があるピクセルだけ金色を返す、それ以外は黒
            half3 goldColor = half3(1.0, 0.84, 0.0) * _GoldIntensity * isGold;
            return half4(goldColor, 1.0);
        }

        // ----------------------------------------------------------
        // Pass 1: ぼかし（9点サンプリングの簡易ガウシアンブラー）
        // 金粉の光を周囲ににじませる
        // ----------------------------------------------------------
        half4 FragGoldBlur(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            float2 texel = _BlitTexture_TexelSize.xy * _GoldBlurSize;

            half4 color = half4(0, 0, 0, 0);
            // 中央（重み4）
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * 4.0;
            // 上下左右（重み2）
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( texel.x, 0)) * 2.0;
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-texel.x, 0)) * 2.0;
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0,  texel.y)) * 2.0;
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0, -texel.y)) * 2.0;
            // 斜め（重み1）
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( texel.x,  texel.y));
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-texel.x,  texel.y));
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( texel.x, -texel.y));
            color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-texel.x, -texel.y));
            // 合計ウェイト = 4 + 2*4 + 1*4 = 16
            color /= 16.0;
            
            // 左下のみ表示されるようにする。

            return color;
        }

        // ----------------------------------------------------------
        // Pass 2: 合成（元の画面 + ぼかした金粉を加算合成）
        // ----------------------------------------------------------
        half4 FragGoldComposite(Varyings input) : SV_Target
        {
            half4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            half4 goldBloom = SAMPLE_TEXTURE2D(_GoldBloomTex, sampler_GoldBloomTex, input.texcoord);
            
            // 金粉だけにグラデーションをかける（左上が強く、右下が弱い）
            float fade = 1.0 - (input.texcoord.x + input.texcoord.y) * 0.5;
            goldBloom *= fade;
            
            // 加算合成
            return original + goldBloom;
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off

        // Pass 0: 金粉抽出
        Pass
        {
            Name "GoldExtract"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragGoldExtract
            ENDHLSL
        }

        // Pass 1: ぼかし
        Pass
        {
            Name "GoldBlur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragGoldBlur
            ENDHLSL
        }

        // Pass 2: 合成
        Pass
        {
            Name "GoldComposite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragGoldComposite
            ENDHLSL
        }
    }
}
