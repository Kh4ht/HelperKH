Shader "KH/UI/SharpBilinear"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Screen Pixels Per Texel", Float) = 1
        _TexSize ("Texture Size", Vector) = (1, 1, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _TexSize;
            float _Scale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float k = max(_Scale, 1.0);
                float2 texel = i.uv * _TexSize.xy;
                float2 floored = floor(texel);
                float2 d = frac(texel) - 0.5;

                // Flat colour inside each texel, one screen pixel of blend at the edges.
                float range = 0.5 - 0.5 / k;
                float2 f = (d - clamp(d, -range, range)) * k + 0.5;

                float3 c = tex2D(_MainTex, (floored + f) / _TexSize.xy).rgb;
                return float4(c, 1.0);
            }
            ENDCG
        }
    }
}