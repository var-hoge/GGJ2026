// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/SparkleImage"
{
	Properties
	{
	[PerRendererData] _MainTex ("Sprite Texture", 2D) = "black" {}
	_Color ("Tint", Color) = (1,1,1,1)
    _StencilComp ("Stencil Comparison", Float) = 8
    _Stencil ("Stencil ID", Float) = 0
    _StencilOp ("Stencil Operation", Float) = 0
    _StencilWriteMask ("Stencil Write Mask", Float) = 255
    _StencilReadMask ("Stencil Read Mask", Float) = 255
	_SparkleCount ("Sparkle Count", Float) = 10
    _SparkleSpeed     ("Sparkle Speed", Float) = 1
    _SparkleSharpness ("Sparkle Sharpness", Float) = 5
    _SparkleIntensity ("Sparkle Intensity", Float) = 1
    _SparkleColor     ("Sparkle Color", Color) = (1,1,1,1)
	_Width ("Screen Width", Float) = 0
	_Height ("Screen Height", Float) = 0

	_ColorIntensity ("_Color Intensity", Float) = 5
    _ColorMask ("Color Mask", Float) = 15
    [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
}

SubShader
{
    Tags
    {
        "Queue"="Transparent"
        "IgnoreProjector"="True"
        "RenderType"="Transparent"
        "PreviewType"="Plane"
        "CanUseSpriteAtlas"="True"
    }

    Stencil
    {
        Ref [_Stencil]
        Comp [_StencilComp]
        Pass [_StencilOp]
        ReadMask [_StencilReadMask]
        WriteMask [_StencilWriteMask]
    }

    Cull Off
    Lighting Off
    ZWrite Off
    ZTest [unity_GUIZTestMode]
    Blend SrcAlpha OneMinusSrcAlpha
    ColorMask [_ColorMask]

    Pass
    {
        Name "Default"
    HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        // #include "UnityCG.cginc"
        // #include "UnityUI.cginc"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
        #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

        struct appdata_t
        {
            float4 vertex   : POSITION;
            float4 color    : COLOR;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            float4 vertex   : SV_POSITION;
            half4 color    : COLOR;
            float2 texcoord  : TEXCOORD0;
            float4 worldPosition : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        half4 _Color;
        // half4 _TextureSampleAdd;
        float4 _ClipRect;
        float4 _MainTex_ST;
        float _SparkleCount;
        float _SparkleSpeed;
        float _SparkleSharpness;
        float _SparkleIntensity;
        half4 _SparkleColor;
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float _ColorIntensity;
        
        // UnityGet2DClipping の URP用実装
        inline half UnityGet2DClipping(float2 position, float4 clipRect)
        {
            float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
            return inside.x * inside.y;
        }
        
        v2f vert(appdata_t v)
        {
            v2f OUT;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.worldPosition = v.vertex;
            OUT.vertex = TransformObjectToHClip(OUT.worldPosition);
            OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
            OUT.color = v.color * _Color;
            return OUT;
        }
        float _testIntensity;
        half4 frag(v2f IN) : SV_Target
        {
            // half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord);
            half4 color = half4(1, 1, 1, IN.color.a); //
            // -1, -1 → 1, 1 にかけて透明になる
            float4 clipPos = TransformObjectToHClip(IN.worldPosition); //-1 ~ 1
            color.a *= -(clipPos.x + clipPos.y); //余計な箇所を削除
            
            half4 testColor = half4 (1.0, 0.8431372549, 0.0, 1.0);
            // if (clipPos.x > 0.4 && clipPos.x < 0.6 &&
            // clipPos.y > 0.4 && clipPos.y < 0.6)
            // {
            //     color = testColor;
            // }
            // else if (clipPos.x > 0.3 && clipPos.x < 0.7 &&
            //         clipPos.y > 0.3 && clipPos.y < 0.7)
            // {
            //     color = testColor;
            //     color.a *= 0.5;
            // }
            
            color.rgb = testColor.rgb + _testIntensity;
            // UV をグリッド状に分割してセルごとの星を生成
            float2 clipPos2 = IN.texcoord * _SparkleCount; // _SparkleCount: 星の密度（例: 10）
            float2 cell = floor(clipPos2); // 0 ~ 10
            float2 local = frac(clipPos2) - 0.5; // セル内の -0.5 ~ 0.5
            
            // セルごとの疑似乱数（星の位置と時間オフセット）
            float2 rand = frac(sin(float2( //0 ~ 1
                dot(cell, float2(127.1, 311.7)), // 各成分を乗算し、加算値を戻り値に返す
                dot(cell, float2(269.5, 183.3))  //
            )) * 43758.5453);
            
            // セル内の星の位置（ランダムにずらす）
            float2 starPos = rand - 0.5; // -0.5 ~ 0.5
            float dist = length(local - starPos); //-0.5 ~ 0.5 - -0.5 ~ 0.5
            
            // 時間で明滅（セルごとに異なるタイミング）
            float twinkle = sin(_Time.y * _SparkleSpeed + rand.x * 6.2832); // 0 ~ 1
            twinkle = twinkle * 0.5 + 0.5; // 0 ~ 1
            
            // 十字の光芒
            float2 d = abs(local - starPos); // 0 ~ 1
            float cross = exp(-d.x * _SparkleSharpness) + exp(-d.y * _SparkleSharpness); // -1 ~ 0 * 5 → -10 ~ 0
            cross *= exp(-dist * 8.0); // 中心から離れると減衰 (4.0 ~ -4.0) → (-40 ~ 40) → e ^ -40, e ^ 40 → infinityレベル
            
            // 丸い輝き + 十字
            float glow = exp(-dist * _SparkleSharpness * 2.0); // 10 * -0.5 + 0.5 → (-5, 5)
            float sparkle = (glow + cross * 0.5) * twinkle; // ((-5 ~ 5) + (-10 ~ 0) * 0.5) * 0 ~ 1 → 0 ~ (-0.375 ~ 1.25)
            
            color.rgb += sparkle * _SparkleIntensity * _SparkleColor.rgb; // 0 ~ (0.375 ~ 1.25) 
            
            #ifdef UNITY_UI_CLIP_RECT
            color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
            #endif

            #ifdef UNITY_UI_ALPHACLIP
            clip (color.a - 0.001);
            #endif

            return color;
        }
    ENDHLSL
    }
	}
CustomEditor "CustomTitleTmpShaderGUI"
}