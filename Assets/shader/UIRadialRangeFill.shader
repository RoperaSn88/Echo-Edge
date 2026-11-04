// UI/Default をベースに、指定した角度区間だけを描画する UI 用シェーダー
// 区間情報は RadialFillRange コンポーネントが頂点の TEXCOORD1 に書き込む
//   uv1.xy : 回転中心からの相対位置 (矩形の長辺で正規化済み。縦横比は保つ)
//   uv1.z  : 描画開始位置 (真上から時計回り、1 = 360度)
//   uv1.w  : 描画する長さ (0〜1)
Shader "EchoEdge/UI/RadialRangeFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

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
        Blend One OneMinusSrcAlpha
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
                float4 range    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 range         : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.range = v.range;
                return OUT;
            }

            // 区間内なら 1、区間外なら 0 (境界は 1px 程度でなめらかにする)
            half RadialMask(float4 range)
            {
                float fillLength = range.w;
                if (fillLength >= 1.0) return 1.0;
                if (fillLength <= 0.0) return 0.0;

                // 真上を 0 として時計回りに 0〜1、開始位置からの距離 d
                float angle = frac(atan2(range.x, range.y) * (0.5 / UNITY_PI));
                float d = frac(angle - range.z);

                // 中心からの距離に応じた 1px あたりの角度変化量
                float aa = max(fwidth(range.x) + fwidth(range.y), 1e-5) / max(length(range.xy) * UNITY_TWO_PI, 1e-5);

                // 最も近い境界までの角度距離 (区間内は正、区間外は負)
                float dist = d <= fillLength
                    ? min(d, fillLength - d)
                    : -min(d - fillLength, 1.0 - d);
                return saturate(dist / aa + 0.5);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                color.a *= RadialMask(IN.range);

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a;
                return color;
            }
        ENDCG
        }
    }
}
