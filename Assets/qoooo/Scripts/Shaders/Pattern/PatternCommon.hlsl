#ifndef QOOOO_PATTERN_COMMON_INCLUDED
#define QOOOO_PATTERN_COMMON_INCLUDED


struct PatternContext
{
    float beat;
    float time;

    float2 resolution;

    float3 mainColor;
    float3 subColor;
};


float3 PatternColor(
    PatternContext context,
    float selector
)
{
    return lerp(
        context.subColor,
        context.mainColor,
        selector
    );
}

#endif
