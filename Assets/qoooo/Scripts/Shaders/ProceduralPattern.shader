Shader "Custom/ProceduralPattern"
{
    Properties
    {
        _PatternType("Pattern", Int) = 0

        _MainColor("Main Color", Color) = (1, 1, 1, 1)
        _SubColor("Sub Color", Color) = (0, 0, 0, 1)

        _Beat("Beat", Float) = 0

        _Resolution("Resolution", Vector) = (0, 0, 0, 0)

        _Tiling("UV Scale", Vector) = (1, 1, 0, 0)
        _Offset("UV Offset", Vector) = (0, 0, 0, 0)
        _Speed("UV Speed", Vector) = (0, 0, 0, 0)

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

            #include "Util/PatternMath.hlsl"
            #include "Pattern/PatternDispatcher.hlsl"

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


            CBUFFER_START(UnityPerMaterial)
                int _PatternType;

                half4 _MainColor;
                half4 _SubColor;

                float _Beat;

                float4 _Resolution;

                float4 _Tiling;
                float4 _Offset;
                float4 _Speed;

                half _Opacity;

            CBUFFER_END


            Varyings vert(
                Attributes input
            )
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                output.uv =
                    input.uv;

                return output;
            }


            half4 frag(
                Varyings input
            ) : SV_Target
            {
                // ----------------------------------
                // Resolution
                // ----------------------------------

                float2 resolution =
                    _Resolution.xy;

                if (
                    resolution.x <= 0.0
                    || resolution.y <= 0.0
                )
                {
                    resolution =
                        _ScreenParams.xy;
                }


                // ----------------------------------
                // UV
                // ----------------------------------

                float2 uv =
                    input.uv;

                uv *=
                    max(
                        _Tiling.xy,
                        float2(
                            0.0001,
                            0.0001
                        )
                    );

                uv +=
                    _Offset.xy;

                uv +=
                    _Speed.xy
                    * _Time.y;


                // 元GLSLと同じ
                // アスペクト補正
                uv =
                    ApplyAspectCorrection(
                        uv,
                        resolution
                    );


                // ----------------------------------
                // Pattern Context
                // ----------------------------------

                PatternContext context;

                context.beat =
                    _Beat;

                context.time =
                    _Time.y;

                context.resolution =
                    resolution;

                context.mainColor =
                    _MainColor.rgb;

                context.subColor =
                    _SubColor.rgb;


                // ----------------------------------
                // Evaluate
                // ----------------------------------

                float3 patternColor =
                    EvaluateProceduralPattern(
                        _PatternType,
                        uv,
                        context
                    );


                return half4(
                    patternColor,
                    _Opacity
                );
            }
            ENDHLSL
        }
    }
}