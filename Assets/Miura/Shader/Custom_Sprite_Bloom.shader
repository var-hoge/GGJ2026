Shader "Custom/Custom_Sprite_Bloom"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
        
        // Bloom 設定
        _BloomThreshold("Bloom Threshold", Range(0, 1)) = 0.5
        _BloomStrength("Bloom Strength", Range(0, 10)) = 1.0
        _BlurSize ("Blur Size", Float) = 1
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]
        //① 実際のゲーム画面に表示される描画
        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma fragment FragBlur

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };
            
            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _BloomThreshold;   // この明るさ以上のピクセルが発光対象になる
                float _BloomStrength;    // 発光の強さ（倍率）
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }
        float _BlurSize;
        float4 _MainTex_TexelSize;
        // ----------------------------------------------------------
        // Pass 1: ぼかし（9点サンプリングの簡易ガウシアンブラー）
        // ----------------------------------------------------------
        half4 FragBlur(Varyings input) : SV_Target
        {
            float2 uv = input.uv;
            float2 texel = _MainTex_TexelSize.xy * _BlurSize;

            half4 color = half4(0, 0, 0, 0);
            // 中央（重み4）
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv) * 4.0;
            // 上下左右（重み2）
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2( texel.x, 0)) * 2.0;
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2(-texel.x, 0)) * 2.0;
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2(0,  texel.y)) * 2.0;
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2(0, -texel.y)) * 2.0;
            // 斜め（重み1）
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2( texel.x,  texel.y));
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2(-texel.x,  texel.y));
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2( texel.x, -texel.y));
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv + float2(-texel.x, -texel.y));
            // 合計ウェイト = 4 + 2*4 + 1*4 = 16
            color /= 16.0;

            return color;
        }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 color = CommonLitFragment(input, input.color);
                
                // 輝度を計算（この色がどれだけ明るいか）
                half brightness = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
                
                // 明るい部分をさらに増幅して HDR 値にする
                // _BloomStrength が大きいほど強く発光する
                half bloom = max(0, brightness - _BloomThreshold);
                color.rgb += color.rgb * bloom * _BloomStrength;
                
                return color;
            }
            ENDHLSL
        }
        //② 画面には見えない裏方の処理。法線マップの情報を書き込んで、2Dライトが凹凸に沿って正しく当たるようにする。
        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }
        //③ 2Dライティングが使えない状況でのフォールバック。ライト無しでテクスチャをそのまま表示する。
        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
