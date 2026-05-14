Shader "JumpRing/LineGlow"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1.8, 1.5, 0.4, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.8, 0.65, 0.12, 1)
        _CoreWidth ("Core Width", Range(0.01, 0.5)) = 0.05
        _InnerGlowWidth ("Inner Glow Width", Range(0.01, 0.5)) = 0.15
        _GlowPower ("Glow Power", Range(1, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                float _CoreWidth;
                float _InnerGlowWidth;
                float _GlowPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dist = abs(IN.uv.y - 0.5) * 2.0;

                // Hard bright core
                float core = smoothstep(_CoreWidth + 0.04, _CoreWidth, dist);

                // Inner glow - tight around core
                float innerGlow = smoothstep(_InnerGlowWidth + 0.15, _InnerGlowWidth * 0.3, dist);
                innerGlow *= (1.0 - core);

                // Outer glow - wide soft falloff across entire width
                float outerGlow = pow(saturate(1.0 - dist), _GlowPower);
                outerGlow *= (1.0 - core) * (1.0 - innerGlow * 0.5);

                // Edge fade to zero at boundaries
                float edgeFade = smoothstep(1.0, 0.75, dist);

                half4 col = _CoreColor * core
                          + _GlowColor * innerGlow * 0.7
                          + _GlowColor * outerGlow * 0.4;

                col *= IN.color;
                col *= edgeFade;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
