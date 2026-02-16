// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/Image/LightingImage"
{
	Properties
    {
	[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
	_Color ("Tint", Color) = (1,1,1,1)
    _ColorAddValue ("ColorAddValue", Float) = 0
    _StencilComp ("Stencil Comparison", Float) = 8
    _Stencil ("Stencil ID", Float) = 0
    _StencilOp ("Stencil Operation", Float) = 0
    _StencilWriteMask ("Stencil Write Mask", Float) = 255
    _StencilReadMask ("Stencil Read Mask", Float) = 255

    _ColorMask ("Color Mask", Float) = 15
    _RotateRange ("Texture Rotation", Range(0, 360)) = 0

    [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    _GlossSpeed ("Gloss Speed", Float) = 0.5
    _GlossWidth ("Gloss Width", Float) = 0.3
    _GlossIntensity ("Gloss Intensity", Float) = 0.5
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
    CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "UnityCG.cginc"
        #include "UnityUI.cginc"

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
            fixed4 color    : COLOR;
            float2 texcoord  : TEXCOORD0;
            float4 worldPosition : TEXCOORD1;
            float2 lightingTexcoord : TEXCOORD2; //
            UNITY_VERTEX_OUTPUT_STEREO
        };

        sampler2D _MainTex;
        fixed4 _Color;
        float _ColorAddValue;
        fixed4 _TextureSampleAdd;
        float4 _ClipRect;
        float4 _MainTex_ST;

        v2f vert(appdata_t v)
        {
            v2f OUT;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.worldPosition = v.vertex;
            OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
            OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
            OUT.color = v.color * _Color + _ColorAddValue;
            return OUT;
        }
        float _RotateRange;
        float _GlossSpeed;    // Propertiesに追加が必要
        float _GlossWidth;    // 光の帯の幅
        float _GlossIntensity; // 光の強さ
        // sampler2D _TMPTex; //Ren
        
    fixed4 frag(v2f IN) : SV_Target
    {
        half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
        // 左上→右下の光沢アニメーション
        // uv.x + uv.y で左上(0)→右下(2)の対角線方向の値を得る
        float diag = (1.0 - IN.texcoord.x) + IN.texcoord.y; // 0.0 ~ 2.0
        float totalRange = 2.0 + _GlossWidth * 2.0;
        float glossPos = (1.0 - frac(_Time.y * _GlossSpeed)) * totalRange - _GlossWidth;
        
        // 光の帯: glossPosを中心に _GlossWidth の幅で滑らかに減衰
        float gloss = 1.0 - saturate(abs(diag - glossPos) / _GlossWidth); //-1 ~ 1
        gloss = gloss * gloss; // 滑らかな減衰（二乗）
        
        color.rgb += gloss * _GlossIntensity;
        color.a = gloss;
        #ifdef UNITY_UI_CLIP_RECT
        color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
        #endif

        #ifdef UNITY_UI_ALPHACLIP
        clip (color.a - 0.001);
        #endif

        return color;
    }
    ENDCG
    }
	}
}