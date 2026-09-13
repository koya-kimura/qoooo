Shader "Custom/ProceduralPattern"
{
    Properties
    {
        [Enum(Checker, 0, Stripes, 1, Solid, 2)] _PatternType("Pattern", Int) = 0
        _ColorA("Color A", Color) = (0.04, 0.04, 0.06, 1)
        _ColorB("Color B", Color) = (0.25, 0.25, 0.35, 1)
        _Tiling("Tiling", Vector) = (12, 8, 0, 0)
        _Offset("Offset", Vector) = (0, 0, 0, 0)
        _Speed("Speed", Vector) = (0, 0, 0, 0)
        _Opacity("Opacity", Range(0, 1)) = 1
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

            int _PatternType;
            int _PatternChecker;
            int _PatternStripes;
            int _PatternSolid;
            half4 _ColorA;
            half4 _ColorB;
            float4 _Tiling;
            float4 _Offset;
            float4 _Speed;
            half _Opacity;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * max(_Tiling.xy, float2(1.0, 1.0));
                uv += _Offset.xy + _Speed.xy * _Time.y;

                half selector;
                if (_PatternType == _PatternChecker)
                {
                    selector = fmod(floor(uv.x) + floor(uv.y), 2.0);
                }
                else if (_PatternType == _PatternStripes)
                {
                    selector = step(0.5, frac(uv.x));
                }
                else if (_PatternType == _PatternSolid)
                {
                    selector = 0.0;
                }
                else
                {
                    selector = 0.0;
                }

                half4 color = lerp(_ColorA, _ColorB, selector);
                color.a *= _Opacity;
                return color;
            }
            ENDHLSL
        }
    }
}
