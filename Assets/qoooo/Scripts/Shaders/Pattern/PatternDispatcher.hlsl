// MaterialTextureSourceのnumeric pattern IDを1つのcreative pattern実装へ割り当てる。
// IDはProceduralPatternTypeと揃え、出力はstraight RGBにする。
#ifndef QOOOO_PATTERN_DISPATCHER_INCLUDED
#define QOOOO_PATTERN_DISPATCHER_INCLUDED

#include "PatternBasic.hlsl"


#define PATTERN_NOISE_TEXTURE       0
#define PATTERN_DIAGONAL_STRIPE     1
#define PATTERN_VERTICAL_STRIPE     2
#define PATTERN_HORIZONTAL_STRIPE   3
#define PATTERN_WAVE_STRIPE         4
#define PATTERN_CHECKERBOARD        5
#define PATTERN_POLKA_DOT           6
#define PATTERN_SUNBURST            7
#define PATTERN_GRID_LINE           8
#define PATTERN_PSYCHEDELIC_RING    9

#define PATTERN_HAND_DRAWN_GRID     10
#define PATTERN_ORGANIC_DOT         11
#define PATTERN_HALFTONE_CIRCLE     12
#define PATTERN_PAPER_GRAIN         13
#define PATTERN_PLASTER             14
#define PATTERN_INK_SPECKLE         15
#define PATTERN_WAVY_CHECKER        16
#define PATTERN_LINEN               17
#define PATTERN_MARBLE              18
#define PATTERN_ROUGH_STRIPE        19


float3 EvaluateProceduralPattern(int patternType, float2 uv, PatternContext context)
{
    switch (patternType)
    {
    case PATTERN_DIAGONAL_STRIPE: return DiagonalStripePattern(uv, context);
    case PATTERN_VERTICAL_STRIPE: return VerticalStripePattern(uv, context);
    case PATTERN_HORIZONTAL_STRIPE: return HorizontalStripePattern(uv, context);
    case PATTERN_WAVE_STRIPE: return WaveStripePattern(uv, context);
    case PATTERN_CHECKERBOARD: return CheckerboardPattern(uv, context);
    case PATTERN_POLKA_DOT: return PolkaDotPattern(uv, context);
    case PATTERN_SUNBURST: return SunburstPattern(uv, context);
    case PATTERN_GRID_LINE: return GridLinePattern(uv, context);
    case PATTERN_PSYCHEDELIC_RING: return PsychedelicRingPattern(uv, context);

    case PATTERN_HAND_DRAWN_GRID: return HandDrawnGridPattern(uv, context);
    case PATTERN_ORGANIC_DOT: return OrganicDotPattern(uv, context);
    case PATTERN_HALFTONE_CIRCLE: return HalftoneCirclePattern(uv, context);
    case PATTERN_PAPER_GRAIN: return PaperGrainPattern(uv, context);
    case PATTERN_PLASTER: return PlasterPattern(uv, context);
    case PATTERN_INK_SPECKLE: return InkSpecklePattern(uv, context);
    case PATTERN_WAVY_CHECKER: return WavyCheckerPattern(uv, context);
    case PATTERN_LINEN: return LinenPattern(uv, context);
    case PATTERN_MARBLE: return MarblePattern(uv, context);
    case PATTERN_ROUGH_STRIPE: return RoughStripePattern(uv, context);

    case PATTERN_NOISE_TEXTURE: default:
        return NoiseTexturePattern(uv, context);
    }
}

#endif
