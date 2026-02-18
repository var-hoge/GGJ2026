Shader "Custom/IrisIn"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _StartTime("_StartTime", Float) = 5

        // アイリスの進行度（0 = 完全に閉じている、1 = 完全に開いている）
        _Progress("Progress", Range(0, 1)) = 0

        // アイリスの中心位置（UV座標: 0〜1）
        _CenterX("Center X", Range(0, 1)) = 0.5
        _CenterY("Center Y", Range(0, 1)) = 0.5

        // 円の境界のぼかし具合（0 = くっきり、大きいほどぼやける）
        _Softness("Edge Softness", Range(0, 0.2)) = 0.02

        // UI 用の Stencil 等の設定
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Progress;
            float _CenterX;
            float _CenterY;
            float _Softness;
            float _StartTime;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex); //uv座標を検出
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); //
                o.color = v.color * _Color; //
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // アスペクト比補正
                float aspect = _ScreenParams.x / _ScreenParams.y;

                // UV と中心の両方に同じ補正をかける
                float2 uv = float2(i.uv.x * aspect, i.uv.y);
                float2 center = float2(_CenterX * aspect, _CenterY);

                // 正円の距離を計算
                float dist = distance(uv, center);

                // timeDistance のスケールも補正後に合わせる
                // 中心(0.5)から角までの最大距離は約 aspect * 0.5 + 0.5 程度
                // float maxRadius = length(float2(aspect, 1.0));
                float maxRadius = length(float2(aspect, 1.0));
                float elapsed = max(0, _Time.y - _StartTime);  // 開始前は0
                float timeDistance = saturate(elapsed * 0.2) * maxRadius;  // 0.1 = 速度

                if (dist <= timeDistance)
                {
                    col.a = 0;
                }
                else
                {
                    col.a = 1;
                }
                return col;
            }
            ENDHLSL
        }
    }
}
