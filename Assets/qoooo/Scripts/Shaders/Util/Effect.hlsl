#ifndef INCLUDE_HLSL_EFFECT
#define INCLUDE_HLSL_EFFECT

void Invert(inout half4 color)
{
    color = half4(half3(1.0, 1.0, 1.0) - color.rgb, color.a);
}

void Unpremultiply(inout half4 color)
{
    color.rgb = color.a > 0.00001h
                    ? color.rgb / color.a
                    : half3(0.0h, 0.0h, 0.0h);
}

float2 Tiling(float2 uv, float2 grid)
{
    return frac(uv * grid);
}

float2 Mosaic(float2 uv, float2 res, float grid)
{
    float2 cells = max(res / max(grid, 1.0), float2(1.0, 1.0));
    return (floor(uv * cells) + 0.5) / cells;
}

float4 ChromaKey(float4 color, float3 keyColor, float threshold, float softness)
{
    float distanceFromKey = distance(color.rgb, keyColor);
    float safeSoftness = max(softness, 0.00001);
    color.a *= smoothstep(threshold, threshold + safeSoftness, distanceFromKey);

    return color;
}

#endif
