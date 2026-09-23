// 最終合成前に、各Layer内の _MainTex へ1回だけTextureEffectを適用する。
// CREATIVE AREA / 創作領域: flagsとsizing uniformでUV処理を選び、出力はstraight-alpha colorのまま保つ。
Shader "Custom/TextureEffect"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        [HideInInspector] _UvEffect("UV Effect", Int) = 0
        [HideInInspector] _SamplingEffect("Sampling Effect", Int) = 0
        [HideInInspector] _ColorEffect("Color Effect", Int) = 0
        [HideInInspector] _AlphaEffect("Alpha Effect", Int) = 0
        [HideInInspector] _EffectAmount("Effect Amount", Float) = 8
        [HideInInspector] _EffectScale("Effect Scale", Float) = 8
        [HideInInspector] _EffectThreshold("Effect Threshold", Float) = 0.5
        [HideInInspector] _EffectStrength("Effect Strength", Float) = 1
        [HideInInspector] _EffectProbability("Effect Probability", Float) = 0.5
        [HideInInspector] _EffectSeed("Effect Seed", Int) = 0
        [HideInInspector] _EffectDirection("Effect Direction", Vector) = (1, 0, 0, 0)
        [HideInInspector] _EffectGrid("Effect Grid", Vector) = (8, 8, 0, 0)
        [HideInInspector] _EffectThresholds("Effect Thresholds", Vector) = (0.25, 0.5, 0.75, 0)
        [HideInInspector] _EffectColorA("Effect Color A", Color) = (0, 0, 0, 1)
        [HideInInspector] _EffectColorB("Effect Color B", Color) = (1, 1, 1, 1)
        [HideInInspector] _EffectColorC("Effect Color C", Color) = (0.33, 0.33, 0.33, 1)
        [HideInInspector] _EffectColorD("Effect Color D", Color) = (0.66, 0.66, 0.66, 1)
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
            #include "Util/Effect.hlsl"

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
            float2 _TileNum;
            float2 _Offset;
            int _IsPosterize;
            float _PosterizeLevels;
            float4 _TextureSize;
            int _UvEffect;
            int _SamplingEffect;
            int _ColorEffect;
            int _AlphaEffect;
            float _EffectAmount;
            float _EffectScale;
            float _EffectThreshold;
            float _EffectStrength;
            float _EffectProbability;
            uint _EffectSeed;
            float2 _EffectDirection;
            float2 _EffectGrid;
            float3 _EffectThresholds;
            float4 _EffectColorA;
            float4 _EffectColorB;
            float4 _EffectColorC;
            float4 _EffectColorD;

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

                if (_IsTiling != 0) uv = Tiling(OffsetUv(uv, _Offset), _TileNum);
                if (_IsMosaic != 0) uv = Mosaic(uv, _TextureSize.xy, _MosaicSize);

                // Stage 1: coordinate effects are composed in source-space order.
                if (_UvEffect == 1) uv = Tiling(OffsetUv(uv, _Offset), _TileNum);
                else if (_UvEffect == 2) uv = Mirror(OffsetUv(uv, _Offset), _TileNum);
                else if (_UvEffect == 3) uv = Kaleidoscope(uv, _TextureSize.xy, _EffectScale, radians(_EffectAmount));
                else if (_UvEffect == 4) uv = Wave(uv, _TextureSize.xy, _EffectAmount.xx, _EffectScale.xx, _EffectDirection);
                else if (_UvEffect == 5) uv = Mosaic(uv, _TextureSize.xy, _EffectScale);
                else if (_UvEffect == 6) uv = HorizontalSliceUv(uv, _TextureSize.xy, _EffectScale, _EffectAmount, _EffectProbability, _EffectSeed);
                else if (_UvEffect == 7) uv = VerticalSliceUv(uv, _TextureSize.xy, _EffectScale, _EffectAmount, _EffectProbability, _EffectSeed);
                else if (_UvEffect == 8) uv = BlockOffsetUv(uv, _TextureSize.xy, _EffectGrid, _EffectAmount, _EffectProbability, _EffectSeed);
                else if (_UvEffect == 9) uv = BlockRepeatUv(uv, _EffectGrid, _EffectAmount, _EffectProbability, _EffectSeed);
                else if (_UvEffect == 10) uv = NoiseDisplacementUv(uv, _TextureSize.xy, _EffectScale, _EffectAmount, _EffectSeed);

                // Stage 2: one sampling filter reads the transformed source texture.
                float4 color;
                if (_SamplingEffect == 1)
                    color = ChannelOffset(_MainTex, sampler_MainTex, uv, _TextureSize.zw,
                        _EffectDirection * _EffectAmount, float2(0.0, 0.0), -_EffectDirection * _EffectAmount);
                else if (_SamplingEffect == 2)
                    color = ChromaticOffset(_MainTex, sampler_MainTex, uv, _TextureSize.zw, _EffectDirection, _EffectAmount);
                else if (_SamplingEffect == 3)
                    color = PrintMisregistration(_MainTex, sampler_MainTex, uv, _TextureSize.zw,
                        _EffectDirection * _EffectAmount, -_EffectDirection * _EffectAmount,
                        _EffectColorA.rgb, _EffectColorB.rgb);
                else if (_SamplingEffect == 4) color = BoxBlur9(_MainTex, sampler_MainTex, uv, _TextureSize.zw);
                else if (_SamplingEffect == 5) color = GaussianBlur9(_MainTex, sampler_MainTex, uv, _TextureSize.zw);
                else if (_SamplingEffect == 6)
                    color = DirectionalBlur9(_MainTex, sampler_MainTex, uv, _TextureSize.zw, _EffectDirection, _EffectAmount);
                else if (_SamplingEffect == 7)
                    color = Bloom9(_MainTex, sampler_MainTex, uv, _TextureSize.zw, _EffectThreshold, _EffectStrength);
                else if (_SamplingEffect == 8) color = SobelEdge(_MainTex, sampler_MainTex, uv, _TextureSize.zw);
                else if (_SamplingEffect == 9)
                    color = Outline(_MainTex, sampler_MainTex, uv, _TextureSize.zw, _EffectAmount, _EffectColorA.rgb);
                else if (_SamplingEffect == 10)
                    color = Sharpen(_MainTex, sampler_MainTex, uv, _TextureSize.zw, _EffectStrength);
                else if (_SamplingEffect == 11)
                    color = Echo9(_MainTex, sampler_MainTex, uv, _TextureSize.zw,
                        _EffectDirection, _EffectAmount, _EffectProbability);
                else if (_SamplingEffect == 12)
                    color = LumaSmear9(_MainTex, sampler_MainTex, uv, _TextureSize.zw,
                        _EffectDirection, _EffectAmount, _EffectThreshold, _EffectStrength - 1.0);
                else color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Stage 3: exactly one selected color transform, then legacy transforms.
                if (_ColorEffect == 1) color = Invert(color);
                else if (_ColorEffect == 2) color = Posterize(color, _EffectScale);
                else if (_ColorEffect == 3) color = Threshold(color, _EffectThreshold);
                else if (_ColorEffect == 4) color = SoftThreshold(color, _EffectThreshold, _EffectStrength);
                else if (_ColorEffect == 5) color = Duotone(color, _EffectColorA.rgb, _EffectColorB.rgb);
                else if (_ColorEffect == 6) color = Tritone(color, _EffectColorA.rgb, _EffectColorC.rgb, _EffectColorB.rgb, _EffectThreshold);
                else if (_ColorEffect == 7) color = Palette4Luma(color,
                    _EffectColorA.rgb, _EffectColorC.rgb, _EffectColorD.rgb, _EffectColorB.rgb, _EffectThresholds);
                else if (_ColorEffect == 8) color = Solarize(color, _EffectThreshold);
                else if (_ColorEffect == 9) color = Contrast(color, _EffectStrength);
                else if (_ColorEffect == 10) color = Saturation(color, _EffectStrength);
                else if (_ColorEffect == 11) color = Grain(color, uv, _TextureSize.xy, _EffectStrength, _EffectSeed);
                else if (_ColorEffect == 12) color = OrderedDitherMono(color, uv, _TextureSize.xy,
                    _EffectScale, _EffectColorA.rgb, _EffectColorB.rgb);
                else if (_ColorEffect == 13) color = OrderedDitherColor(color, uv, _TextureSize.xy, _EffectScale);
                else if (_ColorEffect == 14) color = HalftoneDot(color, uv, _TextureSize.xy,
                    _EffectScale, radians(_EffectAmount), _EffectColorB.rgb, _EffectColorA.rgb);
                else if (_ColorEffect == 15) color = HalftoneLine(color, uv, _TextureSize.xy,
                    _EffectScale, radians(_EffectAmount), _EffectColorB.rgb, _EffectColorA.rgb);
                else if (_ColorEffect == 16) color = HalftoneCross(color, uv, _TextureSize.xy,
                    _EffectScale, radians(_EffectAmount), _EffectColorB.rgb, _EffectColorA.rgb);

                if (_IsInvert != 0) color = Invert(color);
                if (_IsPosterize != 0) color = Posterize(color, _PosterizeLevels);

                // Stage 4: alpha is last so color/sampling filters cannot restore removed coverage.
                if (_AlphaEffect == 1) color = AlphaThreshold(color, _EffectThreshold);
                else if (_AlphaEffect == 2) color = AlphaNoise(color, uv, _TextureSize.xy, _EffectStrength, _EffectSeed);
                else if (_AlphaEffect == 3) color = AlphaNoiseCutout(color, uv, _TextureSize.xy, _EffectThreshold, _EffectSeed);
                else if (_AlphaEffect == 4) color = AlphaSlice(color, uv, _EffectScale, _EffectProbability, _EffectSeed);
                else if (_AlphaEffect == 5) color = BlockSkipAlpha(color, uv, _EffectGrid, _EffectProbability, _EffectSeed);
                else if (_AlphaEffect == 6) color = AlphaDither(color, uv, _TextureSize.xy, _EffectScale);
                else if (_AlphaEffect == 7) color = AlphaHalftoneDot(color, uv, _TextureSize.xy,
                    _EffectScale, radians(_EffectAmount));

                return color;
            }
            ENDHLSL
        }
    }
}