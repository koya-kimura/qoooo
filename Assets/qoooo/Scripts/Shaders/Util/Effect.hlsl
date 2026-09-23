// Layer-effect helpers for TextureEffect.shader.
//
// Design:
// - UV effects transform coordinates before sampling.
// - Color effects transform already-sampled straight-alpha colors.
// - Sampling effects directly read neighboring texels.
// - Alpha effects explicitly modify alpha.
// - Sampling filters internally use premultiplied accumulation where useful,
//   then convert back to straight alpha.
//
// Dependencies:
// - Pcg.hlsl
// - MathUtil.hlsl

#ifndef INCLUDE_HLSL_EFFECT
#define INCLUDE_HLSL_EFFECT

#include "Pcg.hlsl"
#include "MathUtil.hlsl"


#ifndef QOOOO_EFFECT_EPSILON
#define QOOOO_EFFECT_EPSILON 0.00001
#endif

#ifndef QOOOO_TWO_PI
#define QOOOO_TWO_PI (QOOOO_PI * 2.0)
#endif


float Luma(float3 color)
{
    return Gray(color);
}


float2 RandomSigned2(float2 position, uint seed)
{
    float x = Pcg01(float3(position, seed)).x;

    float y = Pcg01(float3(position, seed ^ 0x9E3779B9u)).x;

    return float2(x, y) * 2.0 - 1.0;
}


// ==================================================
// Procedural noise helpers
// ==================================================

float ValueNoise2D(float2 position, uint seed)
{
    float2 cell = floor(position);

    float2 local = frac(position);

    float2 smoothLocal = local * local * (3.0 - 2.0 * local);

    float n00 = Pcg01(float3(cell + float2(0.0, 0.0), seed)).x;

    float n10 = Pcg01(float3(cell + float2(1.0, 0.0), seed)).x;

    float n01 = Pcg01(float3(cell + float2(0.0, 1.0), seed)).x;

    float n11 = Pcg01(float3(cell + float2(1.0, 1.0), seed)).x;

    float nx0 = lerp(n00, n10, smoothLocal.x);

    float nx1 = lerp(n01, n11, smoothLocal.x);

    return lerp(nx0, nx1, smoothLocal.y);
}


float2 ValueNoise2DVector(float2 position, uint seed)
{
    float x = ValueNoise2D(position, seed);

    float y = ValueNoise2D(position, seed ^ 0x85EBCA6Bu);

    return float2(x, y) * 2.0 - 1.0;
}


// ==================================================
// UV effects
// ==================================================

float2 OffsetUv(float2 uv, float2 offset)
{
    return uv + offset;
}


float2 Tiling(float2 uv, float2 scale)
{
    return frac(uv * scale);
}


// --------------------------------------------------
// Mirror repeat
// --------------------------------------------------

float2 Mirror(float2 uv, float2 scale)
{
    float2 p = frac(uv * max(scale, float2(QOOOO_EFFECT_EPSILON, QOOOO_EFFECT_EPSILON)) * 0.5) * 2.0;

    return 1.0 - abs(p - 1.0);
}


// --------------------------------------------------
// Kaleidoscope
// --------------------------------------------------

float2 Kaleidoscope(float2 uv, float2 resolution, float segments, float rotation)
{
    resolution = max(resolution, float2(1.0, 1.0));

    float2 p = uv - 0.5;

    p.y *= resolution.y / resolution.x;

    float2 polar = XyToPolar(p);

    float segmentAngle = QOOOO_TWO_PI / max(segments, 1.0);

    float angle = polar.x + rotation;

    angle = Mod(angle, segmentAngle);

    angle = abs(angle - segmentAngle * 0.5);

    polar.x = angle - rotation;

    p = PolarToXy(polar);

    p.y *= resolution.x / resolution.y;

    return p + 0.5;
}


// --------------------------------------------------
// Wave
// --------------------------------------------------

float2 Wave(float2 uv, float2 resolution, float2 amplitudePixels, float2 frequency, float2 phase)
{
    float2 offsetPixels;

    offsetPixels.x = sin(uv.y * frequency.x * QOOOO_TWO_PI + phase.x) * amplitudePixels.x;

    offsetPixels.y = sin(uv.x * frequency.y * QOOOO_TWO_PI + phase.y) * amplitudePixels.y;

    return uv + PixelToUv(offsetPixels, resolution);
}


// --------------------------------------------------
// Mosaic
// --------------------------------------------------

float2 Mosaic(float2 uv, float2 resolution, float grid)
{
    float2 cells = max(resolution / max(grid, 1.0), float2(1.0, 1.0));

    return (floor(uv * cells) + 0.5) / cells;
}


// --------------------------------------------------
// Horizontal slice
//
// Horizontal bands shift left / right.
// --------------------------------------------------

float2 HorizontalSliceUv(float2 uv, float2 resolution, float slices, float amountPixels, float probability, uint seed)
{
    float sliceCount = max(slices, 1.0);

    float id = floor(uv.y * sliceCount);

    float random = Pcg01(float3(id, id * 0.731, seed)).x;

    if (random < probability)
    {
        float signedRandom = Pcg01(float3(id, id * 1.913, seed ^ 0x9E3779B9u)).x * 2.0 - 1.0;

        uv.x += signedRandom * amountPixels / max(resolution.x, 1.0);
    }

    return uv;
}


// --------------------------------------------------
// Vertical slice
//
// Vertical bands shift up / down.
// --------------------------------------------------

float2 VerticalSliceUv(float2 uv, float2 resolution, float slices, float amountPixels, float probability, uint seed)
{
    float sliceCount = max(slices, 1.0);

    float id = floor(uv.x * sliceCount);

    float random = Pcg01(float3(id, id * 0.731, seed)).x;

    if (random < probability)
    {
        float signedRandom = Pcg01(float3(id, id * 1.913, seed ^ 0x85EBCA6Bu)).x * 2.0 - 1.0;

        uv.y += signedRandom * amountPixels / max(resolution.y, 1.0);
    }

    return uv;
}


// --------------------------------------------------
// Block offset glitch
// --------------------------------------------------

float2 BlockOffsetUv(float2 uv, float2 resolution, float2 grid, float amountPixels, float probability, uint seed)
{
    grid = max(grid, float2(1.0, 1.0));

    float2 cell = floor(uv * grid);

    float trigger = Pcg01(float3(cell, seed)).x;

    if (trigger < probability)
    {
        float2 randomOffset = RandomSigned2(cell, seed ^ 0xA511E9B3u);

        float2 pixelOffset = round(randomOffset * amountPixels);

        uv += PixelToUv(pixelOffset, resolution);
    }

    return uv;
}


// Legacy / generic alias.
float2 BlockGlitchUv(float2 uv, float2 resolution, float2 grid, float amountPixels, float probability, uint seed)
{
    return BlockOffsetUv(uv, resolution, grid, amountPixels, probability, seed);
}


// --------------------------------------------------
// Block repeat
//
// A selected block samples another neighboring block.
// --------------------------------------------------

float2 BlockRepeatUv(float2 uv, float2 grid, float repeatRange, float probability, uint seed)
{
    grid = max(grid, float2(1.0, 1.0));

    float2 scaled = uv * grid;

    float2 cell = floor(scaled);

    float2 local = frac(scaled);

    float trigger = Pcg01(float3(cell, seed)).x;

    if (trigger < probability)
    {
        float2 jump = round(RandomSigned2(cell, seed ^ 0xC2B2AE35u) * repeatRange);

        cell += jump;
    }

    return (cell + local) / grid;
}


// --------------------------------------------------
// Block skip mask
//
// 1 = keep
// 0 = remove
// --------------------------------------------------

float BlockSkipMask(float2 uv, float2 grid, float probability, uint seed)
{
    grid = max(grid, float2(1.0, 1.0));

    float2 cell = floor(uv * grid);

    float random = Pcg01(float3(cell, seed)).x;

    return random < probability ? 0.0 : 1.0;
}


// --------------------------------------------------
// Procedural noise displacement
// --------------------------------------------------

float2 NoiseDisplacementUv(float2 uv, float2 resolution, float frequency, float amountPixels, uint seed)
{
    float2 displacement = ValueNoise2DVector(uv * max(frequency, 0.001), seed);

    return uv + PixelToUv(displacement * amountPixels, resolution);
}


// --------------------------------------------------
// Legacy Sort helper.
//
// NOTE:
// This is not a real pixel-sort implementation.
// It clamps the left side to x = 0.5.
// --------------------------------------------------

float2 Sort(float2 uv)
{
    uv.x = uv.x < 0.5 ? 0.5 : uv.x;

    return uv;
}


// ==================================================
// Basic color effects
// ==================================================

float4 Invert(float4 color)
{
    color.rgb = 1.0 - color.rgb;

    return color;
}


// --------------------------------------------------
// Posterize
// --------------------------------------------------

float4 Posterize(float4 color, float levels)
{
    float quantizedLevels = max(levels, 2.0);

    float steps = quantizedLevels - 1.0;

    color.rgb = floor(saturate(color.rgb) * steps + 0.5) / steps;

    return color;
}


// --------------------------------------------------
// Threshold
// --------------------------------------------------

float4 Threshold(float4 color, float threshold)
{
    float l = Luma(color.rgb);

    float value = step(threshold, l);

    color.rgb = value.xxx;

    return color;
}


float4 SoftThreshold(float4 color, float threshold, float softness)
{
    float l = Luma(color.rgb);

    float safeSoftness = max(softness, QOOOO_EFFECT_EPSILON);

    float value = smoothstep(threshold - safeSoftness, threshold + safeSoftness, l);

    color.rgb = value.xxx;

    return color;
}


// --------------------------------------------------
// Duotone
// --------------------------------------------------

float4 Duotone(float4 color, float3 shadowColor, float3 highlightColor)
{
    float l = saturate(Luma(color.rgb));

    color.rgb = lerp(shadowColor, highlightColor, l);

    return color;
}


// --------------------------------------------------
// Tritone
// --------------------------------------------------

float4 Tritone(float4 color, float3 shadowColor, float3 midColor, float3 highlightColor, float midpoint)
{
    float l = saturate(Luma(color.rgb));

    midpoint = clamp(midpoint, QOOOO_EFFECT_EPSILON, 1.0 - QOOOO_EFFECT_EPSILON);

    if (l < midpoint)
    {
        float t = QooooSafeDiv(l, midpoint);

        color.rgb = lerp(shadowColor, midColor, t);
    }
    else
    {
        float t = QooooSafeDiv(l - midpoint, 1.0 - midpoint);

        color.rgb = lerp(midColor, highlightColor, t);
    }

    return color;
}


// --------------------------------------------------
// Palette quantization by luminance
// --------------------------------------------------

float4 Palette4Luma(float4 color, float3 color0, float3 color1, float3 color2, float3 color3, float3 thresholds)
{
    float l = saturate(Luma(color.rgb));

    if (l < thresholds.x)
    {
        color.rgb = color0;
    }
    else if (l < thresholds.y)
    {
        color.rgb = color1;
    }
    else if (l < thresholds.z)
    {
        color.rgb = color2;
    }
    else
    {
        color.rgb = color3;
    }

    return color;
}


// --------------------------------------------------
// Solarize
// --------------------------------------------------

float4 Solarize(float4 color, float threshold)
{
    float3 inverted = 1.0 - color.rgb;

    float3 mask = step(threshold.xxx, color.rgb);

    color.rgb = lerp(color.rgb, inverted, mask);

    return color;
}


// --------------------------------------------------
// Contrast
//
// 1 = unchanged
// --------------------------------------------------

float4 Contrast(float4 color, float contrast)
{
    color.rgb = saturate((color.rgb - 0.5) * contrast + 0.5);

    return color;
}


// --------------------------------------------------
// Saturation
//
// Not required by the initial list,
// but useful together with the print effects.
//
// 0 = grayscale
// 1 = unchanged
// --------------------------------------------------

float4 Saturation(float4 color, float saturation)
{
    float gray = Luma(color.rgb);

    color.rgb = lerp(gray.xxx, color.rgb, saturation);

    return color;
}


// --------------------------------------------------
// Grain
// --------------------------------------------------

float4 Grain(float4 color, float2 uv, float2 resolution, float strength, uint seed)
{
    float2 pixel = floor(uv * max(resolution, float2(1.0, 1.0)));

    float noise = Pcg01(float3(pixel, seed)).x * 2.0 - 1.0;

    color.rgb = saturate(color.rgb + noise * strength);

    return color;
}


// ==================================================
// Alpha effects
// ==================================================

float4 AlphaThreshold(float4 color, float threshold)
{
    color.a = step(threshold, color.a);

    return color;
}


// --------------------------------------------------
// Alpha noise
// --------------------------------------------------

float4 AlphaNoise(float4 color, float2 uv, float2 resolution, float strength, uint seed)
{
    float2 pixel = floor(uv * max(resolution, float2(1.0, 1.0)));

    float noise = Pcg01(float3(pixel, seed)).x * 2.0 - 1.0;

    color.a = saturate(color.a + noise * strength);

    return color;
}


// --------------------------------------------------
// Alpha noise cutout
// --------------------------------------------------

float4 AlphaNoiseCutout(float4 color, float2 uv, float2 resolution, float threshold, uint seed)
{
    float2 pixel = floor(uv * max(resolution, float2(1.0, 1.0)));

    float noise = Pcg01(float3(pixel, seed)).x;

    color.a *= step(threshold, noise);

    return color;
}


// --------------------------------------------------
// Alpha slice
// --------------------------------------------------

float4 AlphaSlice(float4 color, float2 uv, float slices, float probability, uint seed)
{
    float sliceCount = max(slices, 1.0);

    float id = floor(uv.y * sliceCount);

    float random = Pcg01(float3(id, id * 0.317, seed)).x;

    if (random < probability)
    {
        color.a = 0.0;
    }

    return color;
}


// --------------------------------------------------
// Block skip applied to alpha
// --------------------------------------------------

float4 BlockSkipAlpha(float4 color, float2 uv, float2 grid, float probability, uint seed)
{
    color.a *= BlockSkipMask(uv, grid, probability, seed);

    return color;
}


// ==================================================
// Ordered dither
// ==================================================

float Bayer4x4(int2 p)
{
    int x = p.x & 3;

    int y = p.y & 3;

    if (y == 0)
    {
        if (x == 0) return 0.0 / 16.0;
        if (x == 1) return 8.0 / 16.0;
        if (x == 2) return 2.0 / 16.0;
        return 10.0 / 16.0;
    }

    if (y == 1)
    {
        if (x == 0) return 12.0 / 16.0;
        if (x == 1) return 4.0 / 16.0;
        if (x == 2) return 14.0 / 16.0;
        return 6.0 / 16.0;
    }

    if (y == 2)
    {
        if (x == 0) return 3.0 / 16.0;
        if (x == 1) return 11.0 / 16.0;
        if (x == 2) return 1.0 / 16.0;
        return 9.0 / 16.0;
    }

    if (x == 0) return 15.0 / 16.0;
    if (x == 1) return 7.0 / 16.0;
    if (x == 2) return 13.0 / 16.0;

    return 5.0 / 16.0;
}


float OrderedDitherMask(float value, float2 uv, float2 resolution, float scale)
{
    float2 pixel = floor(uv * max(resolution, float2(1.0, 1.0)) / max(scale, 1.0));

    float threshold = Bayer4x4(int2(pixel));

    return step(threshold, saturate(value));
}


// --------------------------------------------------
// Monochrome dither
// --------------------------------------------------

float4 OrderedDitherMono(float4 color, float2 uv, float2 resolution, float scale, float3 darkColor, float3 lightColor)
{
    float l = Luma(color.rgb);

    float mask = OrderedDitherMask(l, uv, resolution, scale);

    color.rgb = lerp(darkColor, lightColor, mask);

    return color;
}


// --------------------------------------------------
// RGB dither
// --------------------------------------------------

float4 OrderedDitherColor(float4 color, float2 uv, float2 resolution, float scale)
{
    float2 pixel = floor(uv * max(resolution, float2(1.0, 1.0)) / max(scale, 1.0));

    float threshold = Bayer4x4(int2(pixel));

    color.rgb = step(threshold.xxx, saturate(color.rgb));

    return color;
}


// --------------------------------------------------
// Alpha dither
// --------------------------------------------------

float4 AlphaDither(float4 color, float2 uv, float2 resolution, float scale)
{
    color.a = OrderedDitherMask(color.a, uv, resolution, scale);

    return color;
}


// ==================================================
// Halftone
// ==================================================

float2 HalftoneCellUv(float2 uv, float2 resolution, float frequency, float angle)
{
    resolution = max(resolution, float2(1.0, 1.0));

    float2 p = uv - 0.5;

    p.y *= resolution.y / resolution.x;

    p = Rotate2D(p, angle);

    p *= max(frequency, 1.0);

    return frac(p) - 0.5;
}


// --------------------------------------------------
// Halftone circle / dot
// --------------------------------------------------

float HalftoneDotMask(float2 uv, float2 resolution, float frequency, float angle, float fill)
{
    float2 cellUv = HalftoneCellUv(uv, resolution, frequency, angle);

    float distanceToCenter = length(cellUv);

    float radius = 0.70710678 * sqrt(saturate(fill));

    return 1.0 - step(radius, distanceToCenter);
}


float4 HalftoneDot(float4 color, float2 uv, float2 resolution, float frequency, float angle, float3 paperColor,
                   float3 inkColor)
{
    float l = saturate(Luma(color.rgb));

    float fill = 1.0 - l;

    float mask = HalftoneDotMask(uv, resolution, frequency, angle, fill);

    color.rgb = lerp(paperColor, inkColor, mask);

    return color;
}


// Legacy name.
float4 HalftoneMono(float4 color, float2 uv, float2 resolution, float frequency, float angle, float3 paperColor,
                    float3 inkColor)
{
    return HalftoneDot(color, uv, resolution, frequency, angle, paperColor, inkColor);
}


// --------------------------------------------------
// Halftone line
// --------------------------------------------------

float HalftoneLineMask(float2 uv, float2 resolution, float frequency, float angle, float fill)
{
    float2 cellUv = HalftoneCellUv(uv, resolution, frequency, angle);

    float width = 0.5 * saturate(fill);

    return 1.0 - step(width, abs(cellUv.y));
}


float4 HalftoneLine(float4 color, float2 uv, float2 resolution, float frequency, float angle, float3 paperColor,
                    float3 inkColor)
{
    float fill = 1.0 - saturate(Luma(color.rgb));

    float mask = HalftoneLineMask(uv, resolution, frequency, angle, fill);

    color.rgb = lerp(paperColor, inkColor, mask);

    return color;
}


// --------------------------------------------------
// Halftone cross
// --------------------------------------------------

float HalftoneCrossMask(float2 uv, float2 resolution, float frequency, float angle, float fill)
{
    float2 cellUv = HalftoneCellUv(uv, resolution, frequency, angle);

    float width = 0.5 * saturate(fill);

    float horizontal = 1.0 - step(width, abs(cellUv.y));

    float vertical = 1.0 - step(width, abs(cellUv.x));

    return max(horizontal, vertical);
}


float4 HalftoneCross(float4 color, float2 uv, float2 resolution, float frequency, float angle, float3 paperColor,
                     float3 inkColor)
{
    float fill = 1.0 - saturate(Luma(color.rgb));

    float mask = HalftoneCrossMask(uv, resolution, frequency, angle, fill);

    color.rgb = lerp(paperColor, inkColor, mask);

    return color;
}


// --------------------------------------------------
// Alpha halftone
// --------------------------------------------------

float4 AlphaHalftoneDot(float4 color, float2 uv, float2 resolution, float frequency, float angle)
{
    float mask = HalftoneDotMask(uv, resolution, frequency, angle, color.a);

    color.a *= mask;

    return color;
}


// ==================================================
// Sampling / texture based UV effects
// ==================================================

// --------------------------------------------------
// Texture displacement
//
// displacement RG:
// 0.5 = no movement
// 0   = negative
// 1   = positive
// --------------------------------------------------

float2 TextureDisplacementUv(Texture2D displacementTexture, SamplerState samplerState, float2 uv, float2 resolution,
                             float amountPixels)
{
    float2 displacement = displacementTexture.Sample(samplerState, uv).rg * 2.0 - 1.0;

    return uv + PixelToUv(displacement * amountPixels, resolution);
}


// ==================================================
// Sampling effects
// ==================================================

// --------------------------------------------------
// Channel offset
// --------------------------------------------------

float4 ChannelOffset(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 offsetR,
                     float2 offsetG, float2 offsetB)
{
    float4 center = tex.Sample(samplerState, uv);

    float4 sampleR = tex.Sample(samplerState, uv + texelSize * offsetR);

    float4 sampleG = tex.Sample(samplerState, uv + texelSize * offsetG);

    float4 sampleB = tex.Sample(samplerState, uv + texelSize * offsetB);

    return float4(sampleR.r, sampleG.g, sampleB.b, center.a);
}


// --------------------------------------------------
// Chromatic aberration
// --------------------------------------------------

float4 ChromaticOffset(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 direction,
                       float amountPixels)
{
    float2 dir = SafeNormalize2D(direction);

    return ChannelOffset(tex, samplerState, uv, texelSize, dir * amountPixels, float2(0.0, 0.0), -dir * amountPixels);
}


// --------------------------------------------------
// 2-plate print misregistration
//
// Treats dark areas as ink.
// Useful after / instead of Duotone for riso-like looks.
// --------------------------------------------------

float4 PrintMisregistration(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 offsetA,
                            float2 offsetB, float3 inkColorA, float3 inkColorB)
{
    float4 sourceA = tex.Sample(samplerState, uv + texelSize * offsetA);

    float4 sourceB = tex.Sample(samplerState, uv + texelSize * offsetB);

    float coverageA = (1.0 - saturate(Luma(sourceA.rgb))) * sourceA.a;

    float coverageB = (1.0 - saturate(Luma(sourceB.rgb))) * sourceB.a;

    float outAlpha = coverageA + coverageB - coverageA * coverageB;

    float3 premultiplied = inkColorA * coverageA;

    premultiplied += inkColorB * coverageB * (1.0 - coverageA);

    float3 rgb = outAlpha > QOOOO_EFFECT_EPSILON ? premultiplied / outAlpha : float3(0.0, 0.0, 0.0);

    return float4(rgb, saturate(outAlpha));
}


// ==================================================
// Blur
// ==================================================

// --------------------------------------------------
// 3x3 box blur
// --------------------------------------------------

float4 BoxBlur9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize)
{
    float4 sum = float4(0.0, 0.0, 0.0, 0.0);

    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            float4 sampleColor = tex.Sample(samplerState, uv + float2(x, y) * texelSize);

            sum += ToPremultiplied(sampleColor);
        }
    }

    sum /= 9.0;

    return ToStraightAlpha(sum);
}


// --------------------------------------------------
// Gaussian blur 3x3
//
// 1 2 1
// 2 4 2
// 1 2 1
// --------------------------------------------------

float4 GaussianBlur9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize)
{
    float weights[9] = {1.0, 2.0, 1.0, 2.0, 4.0, 2.0, 1.0, 2.0, 1.0};

    float4 sum = float4(0.0, 0.0, 0.0, 0.0);

    int index = 0;

    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            float4 sampleColor = tex.Sample(samplerState, uv + float2(x, y) * texelSize);

            sum += ToPremultiplied(sampleColor) * weights[index];

            index++;
        }
    }

    sum /= 16.0;

    return ToStraightAlpha(sum);
}


// --------------------------------------------------
// Directional blur
// --------------------------------------------------

float4 DirectionalBlur9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 direction,
                        float distance)
{
    float2 dir = SafeNormalize2D(direction);

    float2 delta = dir * texelSize * distance;

    float4 sum = float4(0.0, 0.0, 0.0, 0.0);

    float totalWeight = 0.0;

    [unroll] for (int i = -4; i <= 4; i++)
    {
        float weight = 1.0 - abs((float)i) / 5.0;

        float4 sampleColor = tex.Sample(samplerState, uv + delta * i);

        sum += ToPremultiplied(sampleColor) * weight;

        totalWeight += weight;
    }

    sum /= max(totalWeight, QOOOO_EFFECT_EPSILON);

    return ToStraightAlpha(sum);
}


// ==================================================
// Bloom
// ==================================================

float4 Bloom9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float threshold, float intensity)
{
    float4 center = tex.Sample(samplerState, uv);

    float4 centerPremultiplied = ToPremultiplied(center);

    float weights[9] = {1.0, 2.0, 1.0, 2.0, 4.0, 2.0, 1.0, 2.0, 1.0};

    float3 bloomPremultiplied = float3(0.0, 0.0, 0.0);

    float bloomAlpha = 0.0;
    float totalWeight = 0.0;

    int index = 0;

    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            float4 sampleColor = tex.Sample(samplerState, uv + float2(x, y) * texelSize);

            float brightness = Luma(sampleColor.rgb);

            float brightMask = saturate(QooooSafeDiv(brightness - threshold, 1.0 - threshold));

            float weight = weights[index];

            float coverage = sampleColor.a * brightMask;

            bloomPremultiplied += sampleColor.rgb * coverage * weight;

            bloomAlpha += coverage * weight;

            totalWeight += weight;

            index++;
        }
    }

    bloomPremultiplied /= max(totalWeight, QOOOO_EFFECT_EPSILON);

    bloomAlpha /= max(totalWeight, QOOOO_EFFECT_EPSILON);

    bloomPremultiplied *= intensity;

    bloomAlpha *= intensity;

    float outAlpha = saturate(center.a + bloomAlpha * (1.0 - center.a));

    float3 outPremultiplied = centerPremultiplied.rgb + bloomPremultiplied;

    float3 rgb = outAlpha > QOOOO_EFFECT_EPSILON ? outPremultiplied / outAlpha : float3(0.0, 0.0, 0.0);

    return float4(rgb, outAlpha);
}


// ==================================================
// Edge / outline
// ==================================================

float4 SobelEdge(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize)
{
    float4 c00 = tex.Sample(samplerState, uv + texelSize * float2(-1.0, -1.0));

    float4 c10 = tex.Sample(samplerState, uv + texelSize * float2(0.0, -1.0));

    float4 c20 = tex.Sample(samplerState, uv + texelSize * float2(1.0, -1.0));

    float4 c01 = tex.Sample(samplerState, uv + texelSize * float2(-1.0, 0.0));

    float4 c21 = tex.Sample(samplerState, uv + texelSize * float2(1.0, 0.0));

    float4 c02 = tex.Sample(samplerState, uv + texelSize * float2(-1.0, 1.0));

    float4 c12 = tex.Sample(samplerState, uv + texelSize * float2(0.0, 1.0));

    float4 c22 = tex.Sample(samplerState, uv + texelSize * float2(1.0, 1.0));

    float s00 = Luma(c00.rgb) * c00.a;

    float s10 = Luma(c10.rgb) * c10.a;

    float s20 = Luma(c20.rgb) * c20.a;

    float s01 = Luma(c01.rgb) * c01.a;

    float s21 = Luma(c21.rgb) * c21.a;

    float s02 = Luma(c02.rgb) * c02.a;

    float s12 = Luma(c12.rgb) * c12.a;

    float s22 = Luma(c22.rgb) * c22.a;

    float gx = -s00 + s20 - 2.0 * s01 + 2.0 * s21 - s02 + s22;

    float gy = -s00 - 2.0 * s10 - s20 + s02 + 2.0 * s12 + s22;

    float edge = saturate(sqrt(gx * gx + gy * gy));

    float alpha = tex.Sample(samplerState, uv).a;

    return float4(edge.xxx, alpha);
}


// --------------------------------------------------
// Alpha silhouette outline
// --------------------------------------------------

float AlphaOutlineMask(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float widthPixels)
{
    float centerAlpha = tex.Sample(samplerState, uv).a;

    float2 offset = texelSize * max(widthPixels, 1.0);

    float surroundingAlpha = 0.0;

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(-1.0, -1.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(0.0, -1.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(1.0, -1.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(-1.0, 0.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(1.0, 0.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(-1.0, 1.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(0.0, 1.0)).a);

    surroundingAlpha = max(surroundingAlpha, tex.Sample(samplerState, uv + offset * float2(1.0, 1.0)).a);

    return saturate(surroundingAlpha - centerAlpha);
}


float4 Outline(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float widthPixels,
               float3 outlineColor)
{
    float4 center = tex.Sample(samplerState, uv);

    float mask = AlphaOutlineMask(tex, samplerState, uv, texelSize, widthPixels);

    float outlineAlpha = mask * (1.0 - center.a);

    float outAlpha = center.a + outlineAlpha;

    float3 premultiplied = center.rgb * center.a + outlineColor * outlineAlpha;

    return float4(outAlpha > QOOOO_EFFECT_EPSILON ? premultiplied / outAlpha : float3(0.0, 0.0, 0.0), outAlpha);
}


// ==================================================
// Sharpen
// ==================================================

float4 Sharpen(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float strength)
{
    float4 center = tex.Sample(samplerState, uv);

    float4 left = tex.Sample(samplerState, uv + texelSize * float2(-1.0, 0.0));

    float4 right = tex.Sample(samplerState, uv + texelSize * float2(1.0, 0.0));

    float4 up = tex.Sample(samplerState, uv + texelSize * float2(0.0, 1.0));

    float4 down = tex.Sample(samplerState, uv + texelSize * float2(0.0, -1.0));

    float3 centerPremul = center.rgb * center.a;

    float3 neighborPremul = left.rgb * left.a + right.rgb * right.a + up.rgb * up.a + down.rgb * down.a;

    float3 sharpenedPremul = centerPremul * (1.0 + 4.0 * strength) - neighborPremul * strength;

    float3 rgb = center.a > QOOOO_EFFECT_EPSILON ? sharpenedPremul / center.a : float3(0.0, 0.0, 0.0);

    return float4(saturate(rgb), center.a);
}


// ==================================================
// Echo / trails
// ==================================================

float4 Echo9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 direction,
             float distancePixels, float decay)
{
    float2 dir = SafeNormalize2D(direction);

    float2 delta = dir * texelSize * distancePixels;

    float4 sum = float4(0.0, 0.0, 0.0, 0.0);

    float totalWeight = 0.0;

    float weight = 1.0;

    [unroll] for (int i = 0; i < 9; i++)
    {
        float4 sampleColor = tex.Sample(samplerState, uv - delta * i);

        sum += ToPremultiplied(sampleColor) * weight;

        totalWeight += weight;

        weight *= saturate(decay);
    }

    sum /= max(totalWeight, QOOOO_EFFECT_EPSILON);

    return ToStraightAlpha(sum);
}


// ==================================================
// Pixel-sort-like smear
//
// This is NOT real sorting.
// It scans nearby samples and selects the brightest
// or darkest sample.
//
// mode:
// >= 0 : brightest
// <  0 : darkest
// ==================================================

float4 LumaSmear9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 direction,
                  float distancePixels, float threshold, float mode)
{
    float2 dir = SafeNormalize2D(direction);

    float2 delta = dir * texelSize * distancePixels / 4.0;

    float4 selected = tex.Sample(samplerState, uv);

    float selectedLuma = Luma(selected.rgb);

    [unroll] for (int i = -4; i <= 4; i++)
    {
        float4 sampleColor = tex.Sample(samplerState, uv + delta * i);

        float sampleLuma = Luma(sampleColor.rgb);

        bool valid = mode >= 0.0 ? sampleLuma >= threshold : sampleLuma <= threshold;

        bool better = mode >= 0.0 ? sampleLuma > selectedLuma : sampleLuma < selectedLuma;

        if (valid && better)
        {
            selected = sampleColor;

            selectedLuma = sampleLuma;
        }
    }

    return selected;
}


float4 PixelSortLike9(Texture2D tex, SamplerState samplerState, float2 uv, float2 texelSize, float2 direction,
                      float distancePixels, float threshold, float mode)
{
    return LumaSmear9(tex, samplerState, uv, texelSize, direction, distancePixels, threshold, mode);
}


#endif
