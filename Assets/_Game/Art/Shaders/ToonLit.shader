Shader "MSRPG/ToonLit"
{
    Properties
    {
        _Color          ("Base Color",       Color)        = (1,1,1,1)
        _MainTex        ("Albedo",           2D)           = "white" {}
        _ShadowColor    ("Shadow Color",     Color)        = (0.25,0.28,0.40,1)
        _ShadowThreshold("Shadow Threshold", Range(0,1))   = 0.35
        _RimColor       ("Rim Color",        Color)        = (0.8,0.9,1.0,1)
        _RimPower       ("Rim Power",        Range(0.5,8)) = 3.5
        _OutlineWidth   ("Outline Width",    Range(0,0.05))= 0.008
        _OutlineColor   ("Outline Color",    Color)        = (0.05,0.05,0.08,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ── 메인 패스 ─────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _ShadowColor;
                half   _ShadowThreshold;
                half4  _RimColor;
                half   _RimPower;
                float  _OutlineWidth;
                half4  _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                Light mainLight = GetMainLight();
                half3 N    = normalize(IN.normalWS);
                half3 V    = normalize(GetCameraPositionWS() - IN.positionWS);
                half  NdotL = dot(N, mainLight.direction);

                // 2단계 램프 (밝음 / 어두움)
                half toon  = NdotL > _ShadowThreshold ? 1.0h : 0.3h;
                half3 lit  = lerp(_ShadowColor.rgb, albedo.rgb, toon) * mainLight.color;

                // 림 라이팅
                half  rim    = 1.0h - saturate(dot(V, N));
                half3 rimLit = _RimColor.rgb * pow(rim, _RimPower) * 0.4h;

                return half4(lit + rimLit, albedo.a);
            }
            ENDHLSL
        }

        // ── 외곽선 패스 (Inverted Hull) ───────────────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex   outlineVert
            #pragma fragment outlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _ShadowColor;
                half   _ShadowThreshold;
                half4  _RimColor;
                half   _RimPower;
                float  _OutlineWidth;
                half4  _OutlineColor;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float3 normOS : NORMAL; };
            struct Varyings   { float4 posCS : SV_POSITION; };

            Varyings outlineVert(Attributes IN)
            {
                Varyings OUT;
                float3 expanded = IN.posOS.xyz + IN.normOS * _OutlineWidth;
                OUT.posCS = TransformObjectToHClip(expanded);
                return OUT;
            }

            half4 outlineFrag(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
