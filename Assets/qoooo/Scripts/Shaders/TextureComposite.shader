Shader "Custom/TextureComposite"
{
    Properties
    {
        _MainTex("Bottom", 2D) = "black" {}
        _OverlayTex("Overlay", 2D) = "black" {}
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

    }
}
