Shader "SlotRogue/UI/Space Card Variants"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [Enum(Starfield,0,ShieldPulse,1,WarningScan,2,NebulaGlass,3)] _Variant ("Variant", Float) = 0
        _HologramColor ("Primary Color", Color) = (0.25, 0.85, 1, 1)
        _SecondaryColor ("Secondary Color", Color) = (0.9, 0.35, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _ScanlineStrength ("Pattern Strength", Range(0, 1)) = 0.35
        _ScanlineCount ("Pattern Density", Range(8, 180)) = 72
        _ScanlineSpeed ("Flow Speed", Range(-8, 8)) = 0.8
        _GlitchStrength ("Distortion Strength", Range(0, 0.04)) = 0.004
        _GlitchRate ("Pulse Rate", Range(0, 32)) = 9
        _RgbSplit ("Chromatic Split", Range(0, 0.02)) = 0.002
        _FlickerStrength ("Flicker Strength", Range(0, 0.4)) = 0.05

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Variant;
            fixed4 _HologramColor;
            fixed4 _SecondaryColor;
            float _Intensity;
            float _ScanlineStrength;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _GlitchStrength;
            float _GlitchRate;
            float _RgbSplit;
            float _FlickerStrength;

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 local = frac(value);
                float2 curve = local * local * (3.0 - 2.0 * local);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, curve.x), lerp(c, d, curve.x), curve.y);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 SampleSprite(float2 uv, fixed4 tint)
            {
                fixed4 color = tex2D(_MainTex, uv) + _TextureSampleAdd;
                return color * tint;
            }

            float EdgeMask(float2 uv)
            {
                float2 edge = min(uv, 1.0 - uv);
                return 1.0 - smoothstep(0.0, 0.12, min(edge.x, edge.y));
            }

            float StarLayer(float2 uv, float time, float scale, float threshold)
            {
                float2 starUv = uv * scale + float2(time * _ScanlineSpeed * 0.18, time * -0.08);
                float2 cell = floor(starUv);
                float2 local = frac(starUv) - 0.5;
                float star = smoothstep(0.13, 0.0, length(local));
                return star * step(threshold, Hash21(cell));
            }

            float3 Starfield(float2 uv, float time)
            {
                float stars =
                    StarLayer(uv, time, max(12.0, _ScanlineCount * 0.35), 1.0 - _ScanlineStrength * 0.55) +
                    StarLayer(uv + float2(0.13, 0.37), time * 1.7, max(18.0, _ScanlineCount * 0.55), 0.94);
                float warp = smoothstep(0.94, 1.0, sin((uv.x * 2.0 + uv.y * _ScanlineCount + time * _ScanlineSpeed) * 6.2831853) * 0.5 + 0.5);
                float edge = EdgeMask(uv);
                return _HologramColor.rgb * (stars * 1.15 + warp * _ScanlineStrength * 0.2 + edge * 0.28);
            }

            float3 ShieldPulse(float2 uv, float time)
            {
                float2 centered = uv - 0.5;
                float radius = length(centered);
                float rings = sin((radius * _ScanlineCount * 1.35 - time * _ScanlineSpeed * 4.0) * 6.2831853);
                float ringMask = smoothstep(0.72, 1.0, rings * 0.5 + 0.5);
                float diagonal = smoothstep(0.78, 1.0, sin(((uv.x + uv.y) * _ScanlineCount * 0.22 + time * _ScanlineSpeed) * 6.2831853) * 0.5 + 0.5);
                float edge = EdgeMask(uv);
                return _HologramColor.rgb * (ringMask * _ScanlineStrength * 0.65 + edge * 0.55) +
                    _SecondaryColor.rgb * diagonal * _ScanlineStrength * 0.22;
            }

            float3 WarningScan(float2 uv, float time)
            {
                float lines = smoothstep(0.65, 1.0, sin((uv.y * _ScanlineCount + time * _ScanlineSpeed * 3.5) * 6.2831853) * 0.5 + 0.5);
                float sweep = 1.0 - smoothstep(0.0, 0.03, abs(frac(uv.y + time * _ScanlineSpeed * 0.18) - 0.5));
                float blink = step(0.55, Hash21(float2(floor(time * max(_GlitchRate, 0.01)), 12.7)));
                float edge = EdgeMask(uv);
                return _HologramColor.rgb * (lines * _ScanlineStrength * 0.65 + edge * 0.5) +
                    _SecondaryColor.rgb * (sweep * 0.85 + blink * 0.08);
            }

            float3 NebulaGlass(float2 uv, float time)
            {
                float2 drift = uv * (_ScanlineCount * 0.05) + float2(time * _ScanlineSpeed * 0.07, -time * _ScanlineSpeed * 0.04);
                float cloud = ValueNoise(drift) * 0.55 + ValueNoise(drift * 2.1 + 17.3) * 0.3 + ValueNoise(drift * 4.7) * 0.15;
                float sparkle = StarLayer(uv + cloud * 0.03, time, max(14.0, _ScanlineCount * 0.22), 0.975);
                float edge = EdgeMask(uv);
                return lerp(_HologramColor.rgb, _SecondaryColor.rgb, cloud) *
                    (cloud * _ScanlineStrength * 0.62 + sparkle * 0.8 + edge * 0.3);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float time = _Time.y;
                float lineIndex = floor(IN.texcoord.y * _ScanlineCount);
                float tick = floor(time * max(_GlitchRate, 0.01));
                float glitchMask = step(0.86, Hash21(float2(lineIndex, tick)));
                float glitchDirection = Hash21(float2(lineIndex + 9.0, tick + 2.0)) * 2.0 - 1.0;

                float2 uv = IN.texcoord;
                uv.x += glitchDirection * _GlitchStrength * glitchMask;

                fixed4 color = SampleSprite(uv, IN.color);
                float rgbOffset = _RgbSplit + abs(glitchDirection * _GlitchStrength * glitchMask) * 0.2;
                color.r = SampleSprite(uv + float2(rgbOffset, 0.0), IN.color).r;
                color.b = SampleSprite(uv - float2(rgbOffset, 0.0), IN.color).b;

                float3 overlay = 0.0;
                if (_Variant < 0.5)
                {
                    overlay = Starfield(IN.texcoord, time);
                }
                else if (_Variant < 1.5)
                {
                    overlay = ShieldPulse(IN.texcoord, time);
                }
                else if (_Variant < 2.5)
                {
                    overlay = WarningScan(IN.texcoord, time);
                }
                else
                {
                    overlay = NebulaGlass(IN.texcoord, time);
                }

                float flicker = 1.0 + (Hash21(float2(tick, lineIndex + 31.0)) - 0.5) * _FlickerStrength;
                color.rgb = (color.rgb + overlay * color.a * _Intensity) * flicker;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
