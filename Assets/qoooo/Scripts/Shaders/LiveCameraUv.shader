Shader "Custom/LiveCameraUv"
{
    Properties
    {
        _MainTex("Camera", 2D) = "black" {}
        _UvScale("UV Scale", Vector) = (1, 1, 0, 0)
        _Pan("Pan", Vector) = (0, 0, 0, 0)
        _Opacity("Opacity", Range(0, 1)) = 1
        _Rotation("Rotation", Int) = 0
        _FlipX("Flip X", Int) = 0
        _FlipY("Flip Y", Int) = 0
        _Roll("Roll", Float) = 0
        _LayoutMode("Layout Mode", Int) = 0
        _Contrast("Contrast", Float) = 1
        _Saturation("Saturation", Float) = 1
        _Exposure("Exposure", Float) = 0
        _Vignette("Vignette", Range(0, 1)) = 0
        _Softness("Softness", Range(0, 1)) = 0
        _Chromatic("Chromatic", Float) = 0
        _ColorBalance("Color Balance", Vector) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
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
            float4 _MainTex_TexelSize;
            float2 _UvScale;
            float2 _Pan;
            float _Opacity;
            int _Rotation;
            int _FlipX;
            int _FlipY;
            float _Roll;
            int _LayoutMode;
            float _Contrast;
            float _Saturation;
            float _Exposure;
            float _Vignette;
            float _Softness;
            float _Chromatic;
            float3 _ColorBalance;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                if (_LayoutMode == 1)
                    uv.x = (1.0 + frac(uv.x * 3.0)) / 3.0;
                else if (_LayoutMode == 2)
                    uv.x = abs(uv.x * 2.0 - 1.0);

                if (_FlipX != 0) uv.x = 1.0 - uv.x;
                if (_FlipY != 0) uv.y = 1.0 - uv.y;

                float2 cropMargin = (1.0 - _UvScale) * 0.5;
                uv = 0.5 + (uv - 0.5) * _UvScale + _Pan * cropMargin;

                float2 centered = uv - 0.5;
                if (_Rotation == 90) uv = float2(-centered.y, centered.x) + 0.5;
                else if (_Rotation == 180) uv = -centered + 0.5;
                else if (_Rotation == 270) uv = float2(centered.y, -centered.x) + 0.5;

                centered = uv - 0.5;
                float sine = sin(_Roll);
                float cosine = cos(_Roll);
                uv = mul(float2x2(cosine, -sine, sine, cosine), centered) + 0.5;
                uv = saturate(uv);

                float2 softOffset = _MainTex_TexelSize.xy * _Softness * 2.0;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (_Softness > 0.001)
                {
                    half4 soft = (
                        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(softOffset.x, 0))) +
                        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv - float2(softOffset.x, 0))) +
                        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(0, softOffset.y))) +
                        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv - float2(0, softOffset.y)))) * 0.25;
                    color = lerp(color, soft, _Softness);
                }

                float chromaticOffset = _Chromatic * _MainTex_TexelSize.x;
                if (chromaticOffset > 0.0)
                {
                    color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                        saturate(uv + float2(chromaticOffset, 0))).r;
                    color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                        saturate(uv - float2(chromaticOffset, 0))).b;
                }

                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                color.rgb = lerp(luminance.xxx, color.rgb, _Saturation);
                color.rgb = (color.rgb - 0.5) * _Contrast + 0.5;
                color.rgb *= exp2(_Exposure) * _ColorBalance;
                float vignette = smoothstep(0.28, 0.72, length(input.uv - 0.5));
                color.rgb *= 1.0 - vignette * _Vignette;
                color.a *= _Opacity;
                return color;
            }
            ENDHLSL
        }
    }
}