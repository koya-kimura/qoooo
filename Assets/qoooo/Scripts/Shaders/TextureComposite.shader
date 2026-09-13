Shader "Custom/TextureComposite"
{
    Properties
    {
        _MainTex("Bottom", 2D) = "black" {}
        _OverlayTex("Overlay", 2D) = "black" {}
        _ChromaKeyColor("Chroma Key Color", Color) = (0, 1, 0, 1)
        _ChromaThreshold("Chroma Threshold", Float) = 0.01
        _ChromaSoftness("Chroma Softness", Float) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Composite"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Utils/Effect.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            int _UseChromaKey;
            half4 _ChromaKeyColor;
            float _ChromaThreshold;
            float _ChromaSoftness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 bottom = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 top = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, input.uv);

                if (_UseChromaKey != 0)
                {
                    top = ChromaKey(top, _ChromaKeyColor.rgb, _ChromaThreshold, _ChromaSoftness);
                }

                half outputAlpha = top.a + bottom.a * (1.0h - top.a);
                half3 premultiplied = top.rgb * top.a
                    + bottom.rgb * bottom.a * (1.0h - top.a);
                half3 outputColor = outputAlpha > 0.0001h
                            ? premultiplied / outputAlpha
                            : half3(0.0h, 0.0h, 0.0h);

                return half4(outputColor, outputAlpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ChromaKey"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Utils/Effect.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _ChromaKeyColor;
            float _ChromaThreshold;
            float _ChromaSoftness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color = ChromaKey(color, _ChromaKeyColor.rgb, _ChromaThreshold, _ChromaSoftness);
                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }
}