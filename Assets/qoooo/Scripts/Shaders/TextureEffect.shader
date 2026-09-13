Shader "Custom/TextureEffect"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
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

            int _IsInvert;
            int _IsMosaic;
            float _MosaicSize;
            int _IsTiling;
            int _TileNum;
            float4 _Tiling;
            float4 _Offset;
            float4 _TextureSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                if (_IsTiling != 0) uv = Tiling(uv, _TileNum);
                if (_IsMosaic != 0) uv = Mosaic(uv, _TextureSize.xy, _MosaicSize);

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                if (_IsInvert != 0) Invert(color);

                return color;
            }
            ENDHLSL
        }
    }
}