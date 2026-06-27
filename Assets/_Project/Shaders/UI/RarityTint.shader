Shader "SlotRogue/UI/Rarity Tint"
{
    // 회색/흑백 슬롯 아트를 등급색으로 칠하고, 테두리(밝은 선)에 두 가지 빛을 더한다.
    //  1) 정적 글로우: 밝은 선 주변으로 번지는 등급색 헤일로.
    //  2) 러닝 라이트: 테두리를 따라 한 바퀴 도는 움직이는 빛(혜성). 시간 기반 애니메이션.
    // 기본 UI 셰이더는 곱셈이라 검정(0)이 색을 곱해도 검정. 여기서는 밝기를 [_DarkFloor,1]로
    // 리매핑해 검정=어두운 등급색, 흰선=밝은 등급색. 등급색은 Image.color(정점색)로 주입.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _DarkFloor ("Dark Floor (검정 영역 밝기)", Range(0, 0.25)) = 0.08
        _Brightness ("Brightness", Range(0.25, 3)) = 1
        _EdgeThreshold ("Edge Threshold (테두리 밝기 기준)", Range(0, 1)) = 0.5

        [Header(Running Light)]
        _FlowStrength ("Flow Strength", Range(0, 6)) = 2.5
        _FlowSpeed ("Flow Speed", Range(-4, 4)) = 0.6
        _FlowTail ("Flow Tail (클수록 짧은 꼬리)", Range(1, 24)) = 8
        _FlowCount ("Flow Count (빛 개수)", Range(1, 4)) = 1

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
        // premultiplied alpha: 카드는 정상 합성, 글로우/러닝라이트는 위로 가산.
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.5
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
            float _DarkFloor;
            float _Brightness;
            float _EdgeThreshold;
            float _FlowStrength;
            float _FlowSpeed;
            float _FlowTail;
            float _FlowCount;

            static const float PI = 3.14159265;

            float Luma(float3 rgb)
            {
                return dot(rgb, float3(0.299, 0.587, 0.114));
            }

            // 한 점의 "테두리 발광량": 임계값 위 밝기 × 알파(불투명 밝은 픽셀만).
            float Edge(float2 uv)
            {
                fixed4 t = tex2D(_MainTex, uv) + _TextureSampleAdd;
                float l = Luma(t.rgb) * t.a;
                return saturate((l - _EdgeThreshold) / max(1.0 - _EdgeThreshold, 1e-4));
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color; // = Image.color = 슬롯 등급색
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;

                // 베이스: 회색 아트를 등급색 명암으로 칠함.
                float lum = Luma(tex.rgb);
                float shade = lerp(_DarkFloor, 1.0, saturate(lum)) * _Brightness;
                float3 baseRgb = IN.color.rgb * shade;
                float baseA = tex.a * IN.color.a;

                float edgeHere = Edge(IN.texcoord);

                // 러닝 라이트: 중심 기준 각도로 테두리를 한 바퀴 도는 혜성.
                float2 c = IN.texcoord - 0.5;
                float ang = atan2(c.y, c.x) / (2.0 * PI) + 0.5; // [0,1]
                float phase = frac(ang * _FlowCount - _Time.y * _FlowSpeed);
                float comet = pow(phase, _FlowTail); // 선두에서 밝고 뒤로 꼬리.
                float flow = comet * edgeHere * _FlowStrength;

                float3 addRgb = IN.color.rgb * flow;

                // premultiplied 출력: 베이스는 알파로 곱, 빛은 가산.
                fixed4 result;
                result.rgb = baseRgb * baseA + addRgb;
                result.a = baseA;

                #ifdef UNITY_UI_CLIP_RECT
                float clip2d = UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                result.rgb *= clip2d;
                result.a *= clip2d;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
