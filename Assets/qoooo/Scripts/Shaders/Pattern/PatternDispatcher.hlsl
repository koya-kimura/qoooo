// MaterialTextureSourceのnumeric pattern IDを1つのcreative pattern実装へ割り当てる。
// IDはProceduralPatternTypeと揃え、出力はstraight RGBにする。
#ifndef QOOOO_PATTERN_DISPATCHER_INCLUDED
#define QOOOO_PATTERN_DISPATCHER_INCLUDED

#include "PatternBasic.hlsl"


#define PATTERN_STRIPE          0
#define PATTERN_CHECKER         1
#define PATTERN_WAVE            2
#define PATTERN_INFINITE_RINGS  3
#define PATTERN_POLKA_DOT       4
#define PATTERN_FLOWING_GRID    5
#define PATTERN_SPEED_LINES     6
#define PATTERN_PIXEL_RAIN      7


float3 EvaluateProceduralPattern(int patternType, float2 uv, PatternContext context)
{
    switch (patternType)
    {
    case PATTERN_CHECKER: return CheckerPattern(uv, context);
    case PATTERN_WAVE: return WavePattern(uv, context);
    case PATTERN_INFINITE_RINGS: return InfiniteRingsPattern(uv, context);
    case PATTERN_POLKA_DOT: return BeatPolkaDotPattern(uv, context);
    case PATTERN_FLOWING_GRID: return FlowingGridPattern(uv, context);
    case PATTERN_SPEED_LINES: return SpeedLinesPattern(uv, context);
    case PATTERN_PIXEL_RAIN: return PixelRainPattern(uv, context);
    case PATTERN_STRIPE: default: return StripePattern(uv, context);
    }
}

#endif
