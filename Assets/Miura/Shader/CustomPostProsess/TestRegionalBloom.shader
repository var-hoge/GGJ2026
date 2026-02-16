Shader "Custom/TestRegionalBloom"
{
    // Blit.hlsl を使うため、_BlitTexture がソーステクスチャとして自動バインドされる
    // パラメータは material.SetFloat / SetVector で渡す

    HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Blit.hlsl が頂点シェーダー(Vert)、入力(Attributes)、出力(Varyings)を提供
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Bloom適用領域（UV座標: 0〜1）
        float4 _RegionMin;   // xy を使用
        float4 _RegionMax;   // xy を使用
        // 輝度しきい値
        float _Threshold;
        // Bloom の強さ
        float _Intensity;
        // ぼかしのサンプル距離
        float _BlurSize;
        // ぼかし済みBloomテクスチャ（合成パスで使用）
        TEXTURE2D(_BloomTex);
        SAMPLER(sampler_BloomTex);
        // PostProsessを反映させる
        float4 _ValidPosArray[5];
        int _ValidPosCount;
        
        float _Flickering;

        // ----------------------------------------------------------
        // Pass 0: 輝度抽出（指定矩形内の明るいピクセルだけを取り出す）
        // ----------------------------------------------------------
        half4 FragExtract(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

            // いずれかの矩形内にあるか判定
            bool inRegion = false;
            for (int i = 0; i < _ValidPosCount; i++)
            {
                float4 r = _ValidPosArray[i];
                // r.xy = 左下, r.zw = 右上
                if (uv.x >= r.x && uv.x <= r.z && uv.y >= r.y && uv.y <= r.w)
                {
                    inRegion = true;
                    break;
                }
            }

            // 矩形外なら黒（Bloomなし）
            if (!inRegion)
                return half4(0, 0, 0, 0);

            // 輝度を計算し、しきい値以下なら黒にする
            half brightness = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
            half contribution = max(0, brightness - _Threshold);
            return color * contribution;
        }
        // ----------------------------------------------------------
        // Pass 1: ぼかし（9点サンプリングの簡易ガウシアンブラー）
        // ----------------------------------------------------------
        half4 FragBlur(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            float2 texel = _BlitTexture_TexelSize.xy * _BlurSize;

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

            return color;
        }
        // ----------------------------------------------------------
        // Pass 2: 合成（元の画面 + ぼかしたBloom を加算合成）
        // ----------------------------------------------------------
        half4 FragComposite(Varyings input) : SV_Target
        {
            half4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            half4 bloom = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, input.texcoord);
            float timeMultiplier = 0.5 + 0.5 * sin(_Time.y * _Flickering);
            return original + bloom * _Intensity * timeMultiplier;
            // return original * _Intensity;
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off

        // Pass 0: 輝度抽出
        Pass
        {
            Name "BrightExtract"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragExtract
            ENDHLSL
        }

        // Pass 1: ぼかし
        Pass
        {
            Name "Blur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlur
            ENDHLSL
        }

        // Pass 2: 合成
        Pass
        {
            Name "Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL
        }
    }
}
