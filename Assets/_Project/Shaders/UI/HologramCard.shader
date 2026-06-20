Shader "SlotRogue/UI/Hologram Card"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _HologramColor ("Hologram Color", Color) = (0.22, 0.88, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.28
        _ScanlineCount ("Scanline Count", Range(8, 180)) = 96
        _ScanlineSpeed ("Scanline Speed", Range(-8, 8)) = 1.35
        _GlitchStrength ("Glitch Strength", Range(0, 0.04)) = 0.008
        _GlitchRate ("Glitch Rate", Range(0, 32)) = 11
        _RgbSplit ("RGB Split", Range(0, 0.02)) = 0.003
        _FlickerStrength ("Flicker Strength", Range(0, 0.4)) = 0.08

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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            fixed4 _HologramColor;
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
                value = frac(value * float2(123.34, 345.45));
                value += dot(value, value + 34.345);
                return frac(value.x * value.y);
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

            fixed4 frag(v2f IN) : SV_Target
            {
                float time = _Time.y;
                float lineIndex = floor(IN.texcoord.y * _ScanlineCount);
                float tick = floor(time * max(_GlitchRate, 0.01));

                float glitchMask = step(0.82, Hash21(float2(lineIndex, tick)));
                float glitchDirection = Hash21(float2(lineIndex + 17.0, tick + 3.0)) * 2.0 - 1.0;
                float2 uv = IN.texcoord;
                uv.x += glitchDirection * _GlitchStrength * glitchMask;

                fixed4 color = SampleSprite(uv, IN.color);
                float rgbOffset = _RgbSplit + abs(glitchDirection * _GlitchStrength * glitchMask) * 0.25;
                color.r = SampleSprite(uv + float2(rgbOffset, 0.0), IN.color).r;
                color.b = SampleSprite(uv - float2(rgbOffset, 0.0), IN.color).b;

                float scanWave = sin((IN.texcoord.y * _ScanlineCount + time * _ScanlineSpeed) * 6.2831853);
                float scanline = smoothstep(0.55, 1.0, scanWave * 0.5 + 0.5);
                float flicker = 1.0 + (Hash21(float2(tick, lineIndex + 71.0)) - 0.5) * _FlickerStrength;

                float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
                float brightStroke = smoothstep(0.08, 0.42, luminance) * color.a;
                float verticalGlow = smoothstep(0.0, 0.09, IN.texcoord.y) *
                    (1.0 - smoothstep(0.88, 1.0, IN.texcoord.y));

                float holoAmount = saturate((brightStroke + scanline * _ScanlineStrength + glitchMask * 0.2) * _Intensity);
                color.rgb += _HologramColor.rgb * holoAmount * color.a;
                color.rgb += _HologramColor.rgb * verticalGlow * 0.08 * _Intensity * color.a;
                color.rgb *= flicker;

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
