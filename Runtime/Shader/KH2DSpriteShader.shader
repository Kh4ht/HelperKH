// MasterSpriteShader.shader
// A single "master" URP sprite shader with many independently toggleable effects.
// Target: Universal Render Pipeline, 2D Renderer, Unlit sprites.
// Drop this file anywhere in Assets/, then create a Material using
// Shader > Custom > 2D > Master Sprite Shader, and assign it to your SpriteRenderer.
//
// All effects are compiled via shader_feature_local, so any effect you leave
// disabled on a given material is stripped from the build - you only pay for
// what you actually turn on.

Shader "KH/KH2D Sprite Shader"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor]   _Color ("Tint Color", Color) = (1,1,1,1)

        // Required for SpriteRenderer.color / sprite atlas support - do not remove.
        [PerRendererData] _RendererColor ("RendererColor (internal)", Color) = (1,1,1,1)
        [PerRendererData] _Flip ("Flip (internal)", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha (internal)", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("EnableExternalAlpha (internal)", Float) = 0

        [Space]
        [Header(ALPHA CUTOFF)]
        [Toggle(_ALPHACUTOFF_ON)] _EnableAlphaCutoff ("Enable Alpha Cutoff", Float) = 0
        _AlphaCutoff ("Cutoff Threshold", Range(0,1)) = 0.1

        [Space]
        [Header(OUTER OUTLINE)]
        [Toggle(_OUTLINE_ON)] _EnableOutline ("Enable Outer Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (px)", Range(0,10)) = 1
        [Toggle] _OutlineOnly ("Show Outline Only", Float) = 0

        [Space]
        [Header(INNER OUTLINE GLOW)]
        [Toggle(_INNEROUTLINE_ON)] _EnableInnerOutline ("Enable Inner Outline", Float) = 0
        _InnerOutlineColor ("Inner Outline Color", Color) = (1,1,1,1)
        _InnerOutlineWidth ("Inner Outline Width (px)", Range(0,10)) = 1

        [Space]
        [Header(DISSOLVE)]
        [Toggle(_DISSOLVE_ON)] _EnableDissolve ("Enable Dissolve", Float) = 0
        _DissolveNoiseTex ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _DissolveEdgeWidth ("Edge Width", Range(0,0.5)) = 0.05
        _DissolveEdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        [Toggle] _DissolveInvert ("Invert Direction", Float) = 0

        [Space]
        [Header(FLASH HIT FEEDBACK)]
        [Toggle(_FLASH_ON)] _EnableFlash ("Enable Flash", Float) = 0
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0

        [Space]
        [Header(FILL SILHOUETTE RECOLOR)]
        [Toggle(_FILL_ON)] _EnableFill ("Enable Fill", Float) = 0
        _FillColor ("Fill Color", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0,1)) = 0

        [Space]
        [Header(HUE SATURATION BRIGHTNESS CONTRAST)]
        [Toggle(_HSBC_ON)] _EnableHSBC ("Enable Hue/Sat/Bright/Contrast", Float) = 0
        _Hue ("Hue Shift", Range(-180,180)) = 0
        _Saturation ("Saturation", Range(0,2)) = 1
        _Brightness ("Brightness", Range(-1,1)) = 0
        _Contrast ("Contrast", Range(0,2)) = 1

        [Space]
        [Header(GRAYSCALE)]
        [Toggle(_GRAYSCALE_ON)] _EnableGrayscale ("Enable Grayscale", Float) = 0
        _GrayscaleAmount ("Grayscale Amount", Range(0,1)) = 0

        [Space]
        [Header(EDGE GLOW RIM)]
        [Toggle(_RIM_ON)] _EnableRim ("Enable Edge Glow", Float) = 0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimWidth ("Rim Width (px)", Range(0,20)) = 4
        _RimIntensity ("Rim Intensity", Range(0,5)) = 1

        [Space]
        [Header(SHINE SWEEP)]
        [Toggle(_SHINE_ON)] _EnableShine ("Enable Shine Sweep", Float) = 0
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineWidth ("Shine Band Width", Range(0.01,1)) = 0.15
        _ShineAngle ("Shine Angle (deg)", Range(0,180)) = 30
        _ShineSpeed ("Shine Speed", Range(0,5)) = 1
        _ShineIntensity ("Shine Intensity", Range(0,5)) = 1
        [Toggle] _ShineLoop ("Loop Continuously", Float) = 1

        [Space]
        [Header(CHROMATIC ABERRATION)]
        [Toggle(_CHROMATIC_ON)] _EnableChromatic ("Enable Chromatic Aberration", Float) = 0
        _ChromaticAmount ("Amount (px)", Range(0,10)) = 1

        [Space]
        [Header(PIXELATION)]
        [Toggle(_PIXELATE_ON)] _EnablePixelate ("Enable Pixelation", Float) = 0
        _PixelSize ("Pixel Block Size (px)", Range(1,64)) = 8

        [Space]
        [Header(WAVE DISTORTION)]
        [Toggle(_WAVE_ON)] _EnableWave ("Enable Wave Distortion", Float) = 0
        _WaveAmplitude ("Amplitude (UV)", Range(0,0.1)) = 0.01
        _WaveFrequency ("Frequency", Range(0,50)) = 10
        _WaveSpeed ("Speed", Range(0,10)) = 2
        [Toggle] _WaveVertical ("Vertical Waves", Float) = 0

        [Space]
        [Header(RENDER SETTINGS)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite [_ZWrite]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "MasterSpriteUnlit"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #pragma shader_feature_local _ALPHACUTOFF_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma shader_feature_local _INNEROUTLINE_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _FLASH_ON
            #pragma shader_feature_local _FILL_ON
            #pragma shader_feature_local _HSBC_ON
            #pragma shader_feature_local _GRAYSCALE_ON
            #pragma shader_feature_local _RIM_ON
            #pragma shader_feature_local _SHINE_ON
            #pragma shader_feature_local _CHROMATIC_ON
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _WAVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            TEXTURE2D(_AlphaTex);      SAMPLER(sampler_AlphaTex);
            float  _EnableExternalAlpha;

            TEXTURE2D(_DissolveNoiseTex); SAMPLER(sampler_DissolveNoiseTex);

            half4 _Color;
            half4 _RendererColor;
            float4 _Flip;

            float  _AlphaCutoff;

            half4  _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineOnly;

            half4  _InnerOutlineColor;
            float  _InnerOutlineWidth;

            float  _DissolveAmount;
            float  _DissolveEdgeWidth;
            half4  _DissolveEdgeColor;
            float  _DissolveInvert;

            half4  _FlashColor;
            float  _FlashAmount;

            half4  _FillColor;
            float  _FillAmount;

            float  _Hue;
            float  _Saturation;
            float  _Brightness;
            float  _Contrast;

            float  _GrayscaleAmount;

            half4  _RimColor;
            float  _RimWidth;
            float  _RimIntensity;

            half4  _ShineColor;
            float  _ShineWidth;
            float  _ShineAngle;
            float  _ShineSpeed;
            float  _ShineIntensity;
            float  _ShineLoop;

            float  _ChromaticAmount;

            float  _PixelSize;

            float  _WaveAmplitude;
            float  _WaveFrequency;
            float  _WaveSpeed;
            float  _WaveVertical;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                IN.positionOS.xy *= _Flip.xy;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.color = IN.color * _Color * _RendererColor;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                #if defined(PIXELSNAP_ON)
                float4 pixelPos = OUT.positionCS * _ScreenParams.y / 2.0;
                pixelPos.xy = floor(pixelPos.xy);
                OUT.positionCS = pixelPos * 2.0 / _ScreenParams.y;
                #endif

                return OUT;
            }

            // ---- helpers ----

            float3 RgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float SampleAlphaRaw(float2 uv)
            {
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                #if ETC1_EXTERNAL_ALPHA
                a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                #endif
                return a;
            }

            // Max alpha found among 8 neighbours at a given pixel-space radius.
            float MaxNeighbourAlpha(float2 uv, float radiusPx)
            {
                float2 texel = _MainTex_TexelSize.xy * radiusPx;
                float maxA = 0;
                const float2 dirs[8] = {
                    float2( 1, 0), float2(-1, 0), float2(0, 1), float2(0,-1),
                    float2( 0.7071, 0.7071), float2(-0.7071, 0.7071),
                    float2( 0.7071,-0.7071), float2(-0.7071,-0.7071)
                };
                UNITY_UNROLL
                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + dirs[i] * texel;
                    maxA = max(maxA, SampleAlphaRaw(sampleUV));
                }
                return maxA;
            }

            float MinNeighbourAlpha(float2 uv, float radiusPx)
            {
                float2 texel = _MainTex_TexelSize.xy * radiusPx;
                float minA = 1;
                const float2 dirs[8] = {
                    float2( 1, 0), float2(-1, 0), float2(0, 1), float2(0,-1),
                    float2( 0.7071, 0.7071), float2(-0.7071, 0.7071),
                    float2( 0.7071,-0.7071), float2(-0.7071,-0.7071)
                };
                UNITY_UNROLL
                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + dirs[i] * texel;
                    minA = min(minA, SampleAlphaRaw(sampleUV));
                }
                return minA;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // --- UV-space distortions happen first, before sampling color ---
                #if defined(_PIXELATE_ON)
                {
                    float2 blockUV = _PixelSize * _MainTex_TexelSize.xy;
                    uv = floor(uv / blockUV) * blockUV + blockUV * 0.5;
                }
                #endif

                #if defined(_WAVE_ON)
                {
                    float t = _Time.y * _WaveSpeed;
                    if (_WaveVertical > 0.5)
                        uv.y += sin(uv.x * _WaveFrequency + t) * _WaveAmplitude;
                    else
                        uv.x += sin(uv.y * _WaveFrequency + t) * _WaveAmplitude;
                }
                #endif

                // --- base color sample (with optional chromatic aberration) ---
                half4 texColor;
                #if defined(_CHROMATIC_ON)
                {
                    float2 off = _MainTex_TexelSize.xy * _ChromaticAmount;
                    float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + off).r;
                    float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                    float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - off).b;
                    float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                    texColor = half4(r, g, b, a);
                }
                #else
                {
                    texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                }
                #endif

                #if ETC1_EXTERNAL_ALPHA
                texColor.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                #endif

                half4 color = texColor * IN.color;

                // --- dissolve ---
                #if defined(_DISSOLVE_ON)
                {
                    float noise = SAMPLE_TEXTURE2D(_DissolveNoiseTex, sampler_DissolveNoiseTex, uv).r;
                    float amount = _DissolveInvert > 0.5 ? (1.0 - _DissolveAmount) : _DissolveAmount;
                    float diff = noise - amount;
                    clip(diff);
                    float edge = smoothstep(0.0, _DissolveEdgeWidth, diff);
                    color.rgb = lerp(_DissolveEdgeColor.rgb, color.rgb, edge);
                }
                #endif

                // --- alpha cutoff ---
                #if defined(_ALPHACUTOFF_ON)
                clip(color.a - _AlphaCutoff);
                #endif

                // --- hue / saturation / brightness / contrast ---
                #if defined(_HSBC_ON)
                {
                    float3 hsv = RgbToHsv(color.rgb);
                    hsv.x = frac(hsv.x + _Hue / 360.0);
                    hsv.y = saturate(hsv.y * _Saturation);
                    color.rgb = HsvToRgb(hsv);
                    color.rgb = (color.rgb - 0.5) * _Contrast + 0.5 + _Brightness;
                }
                #endif

                // --- grayscale ---
                #if defined(_GRAYSCALE_ON)
                {
                    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
                    color.rgb = lerp(color.rgb, gray.xxx, _GrayscaleAmount);
                }
                #endif

                // --- fill / silhouette recolor ---
                #if defined(_FILL_ON)
                color.rgb = lerp(color.rgb, _FillColor.rgb, _FillAmount * _FillColor.a);
                #endif

                // --- flash hit feedback ---
                #if defined(_FLASH_ON)
                color.rgb = lerp(color.rgb, _FlashColor.rgb, _FlashAmount * _FlashColor.a);
                #endif

                // --- shine sweep ---
                #if defined(_SHINE_ON)
                {
                    float rad = radians(_ShineAngle);
                    float2 dir = float2(cos(rad), sin(rad));
                    float proj = dot(uv - 0.5, dir) + 0.5;
                    float t = frac(_Time.y * _ShineSpeed);
                    float dist = abs(proj - t);
                    if (_ShineLoop < 0.5) dist = abs(proj - saturate(_Time.y * _ShineSpeed));
                    float band = 1.0 - smoothstep(0.0, _ShineWidth, dist);
                    color.rgb += _ShineColor.rgb * band * _ShineIntensity * texColor.a;
                }
                #endif

                // --- edge glow / rim (alpha-based, works without normals) ---
                #if defined(_RIM_ON)
                {
                    float outsideMax = MaxNeighbourAlpha(uv, _RimWidth);
                    float rim = saturate(outsideMax - texColor.a) * _RimIntensity;
                    color.rgb += _RimColor.rgb * rim;
                    color.a = saturate(color.a + rim * _RimColor.a);
                }
                #endif

                // --- inner outline / inner glow ---
                #if defined(_INNEROUTLINE_ON)
                {
                    float neighbourMin = MinNeighbourAlpha(uv, _InnerOutlineWidth);
                    float inner = texColor.a * saturate(texColor.a - neighbourMin);
                    color.rgb = lerp(color.rgb, _InnerOutlineColor.rgb, inner * _InnerOutlineColor.a);
                }
                #endif

                // --- outer outline (drawn last, can extend beyond original alpha) ---
                #if defined(_OUTLINE_ON)
                {
                    float neighbourMax = MaxNeighbourAlpha(uv, _OutlineWidth);
                    float isOutline = saturate(neighbourMax - texColor.a);
                    if (isOutline > 0.001)
                    {
                        color.rgb = lerp(color.rgb, _OutlineColor.rgb, isOutline);
                        color.a = max(color.a, isOutline * _OutlineColor.a);
                    }
                    if (_OutlineOnly > 0.5)
                    {
                        color.rgb = _OutlineColor.rgb;
                        color.a = isOutline * _OutlineColor.a;
                    }
                }
                #endif

                return color;
            }
            ENDHLSL
        }
    }

    CustomEditor "MasterSpriteShaderGUI"
    Fallback "Sprites/Default"
}
