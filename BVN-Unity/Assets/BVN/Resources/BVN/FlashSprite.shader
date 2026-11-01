// Flash-style sprite: per-vertex colour transform (multiplier in COLOR, offset in TEXCOORD1),
// premultiplied output, configurable blend / stencil for Flash blend modes and masks.
Shader "BVN/FlashSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SrcBlend ("Src", Float) = 1
        _DstBlend ("Dst", Float) = 10
        _BlendOp ("Op", Float) = 0
        _ColorMask ("ColorMask", Float) = 15
        _StencilRef ("StencilRef", Float) = 0
        _StencilComp ("StencilComp", Float) = 8
        _StencilOp ("StencilOp", Float) = 0
        _TextMode ("TextMode", Float) = 0
        _Darken ("Darken", Float) = 0
        _AlphaClip ("AlphaClip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off
        Blend [_SrcBlend] [_DstBlend]
        BlendOp [_BlendOp]
        ColorMask [_ColorMask]
        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilOp]
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TextMode;
            float _Darken;
            float _AlphaClip;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 add : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 add : TEXCOORD1;
                float4 mul : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.add = v.add;
                o.mul = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                if (_TextMode > 0.5) t = float4(1, 1, 1, t.a);
                float4 c = saturate(t * i.mul + i.add);
                if (_AlphaClip > 0.5) clip(c.a - 0.01);
                // Flash colour transforms apply to straight colour; output premultiplied
                c.rgb *= c.a;
                if (_Darken > 0.5) c.rgb += (1 - c.a);
                return c;
            }
            ENDCG
        }
    }
}
