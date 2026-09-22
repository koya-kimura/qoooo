// Layer-effect helpers transform UVs or straight-alpha sampled color for TextureEffect.shader.
#ifndef INCLUDE_HLSL_EFFECT
#define INCLUDE_HLSL_EFFECT

void Invert(inout half4 color)
{
    color = half4(half3(1.0, 1.0, 1.0) - color.rgb, color.a);
}

void Posterize(inout half4 color, float levels)
{
    float quantizedLevels = max(levels, 2.0);
    color.rgb = floor(saturate(color.rgb) * quantizedLevels) / max(quantizedLevels - 1.0, 1.0);
}

void Unpremultiply(inout half4 color)
{
    color.rgb = color.a > 0.00001h
                    ? color.rgb / color.a
                    : half3(0.0h, 0.0h, 0.0h);
}

// TilingはPoster上の複数配置ではなく、1枚のsource texture内でUVを繰り返す処理。
float2 Tiling(float2 uv, float2 scale, float2 offset)
{
    return frac(uv * scale + offset);
}

float2 Mosaic(float2 uv, float2 res, float grid)
{
    float2 cells = max(res / max(grid, 1.0), float2(1.0, 1.0));
    return (floor(uv * cells) + 0.5) / cells;
}

#endif
