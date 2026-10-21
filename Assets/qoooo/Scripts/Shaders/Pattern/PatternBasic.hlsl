// CREATIVE AREA / 創作領域: 各procedural Background patternはUVとPatternContextを受け取る。
// 各functionはPatternDispatcherが選んだstraight RGB colorを返す。
#ifndef QOOOO_PATTERN_BASIC_INCLUDED
#define QOOOO_PATTERN_BASIC_INCLUDED

#include "../Util/Pcg.hlsl"
#include "../Util/MathUtil.hlsl"
#include "PatternCommon.hlsl"


float PatternHash(float2 position, float seed)
{
    return Pcg01(float3(position, seed)).x;
}


float PatternCircleMask(float2 position, float radius)
{
    float distanceFromCenter = length(position);
    float aa = max(fwidth(distanceFromCenter), 0.0005);

    return 1.0 - smoothstep(radius - aa, radius + aa, distanceFromCenter);
}


// 0: beatごとにランダムな横線がactiveになるstripe。
float3 StripePattern(float2 uv, PatternContext context)
{
    float linePosition = uv.y * 2.0;
    float lineIndex = floor(linePosition);
    float distanceToCenter = abs(frac(linePosition) - 0.5);
    float lineAA = max(fwidth(linePosition), 0.001);
    float lineMask = 1.0 - smoothstep(0.12, 0.12 + lineAA, distanceToCenter);
    float beatIndex = floor(context.beat);
    float active = step(0.42, PatternHash(float2(lineIndex, beatIndex), 17.0));
    float beatAccent = 1.0 - smoothstep(0.0, 0.28, frac(context.beat));
    float pattern = lineMask * active * lerp(0.58, 1.0, beatAccent);

    return PatternColor(context, pattern);
}


// 1: 上方向へ流れるchecker。
float3 CheckerPattern(float2 uv, PatternContext context)
{
    float2 position = uv;
    position.y -= context.beat * 0.25;
    float pattern = Mod(floor(position.x) + floor(position.y), 2.0);

    return PatternColor(context, pattern);
}


// 2: 横方向へ流れるsimple sine wave。
float3 WavePattern(float2 uv, PatternContext context)
{
    float movingX = uv.x - context.beat * 0.5;
    float waveCenter = 0.5 + sin(movingX * QOOOO_PI * 2.0) * 0.28;
    float distanceToWave = abs(frac(uv.y) - waveCenter);
    float waveAA = max(fwidth(distanceToWave), 0.001);
    float pattern = 1.0 - smoothstep(0.035, 0.035 + waveAA, distanceToWave);

    return PatternColor(context, pattern);
}


// 3: 画面中央から奥行き方向へ湧き続けるoutline rings。
float3 InfiniteRingsPattern(float2 uv, PatternContext context)
{
    float radius = length(context.screenUv - 0.5);
    float ringPosition = radius * 18.0 - context.beat * 1.5;
    float distanceToRing = abs(frac(ringPosition) - 0.5);
    float ringAA = max(fwidth(ringPosition), 0.001);
    float pattern = 1.0 - smoothstep(0.055, 0.055 + ringAA, distanceToRing);

    return PatternColor(context, pattern);
}


// 4: 行ごとに半cellずらし、beatでsizeが変わるpolka dots。
float3 BeatPolkaDotPattern(float2 uv, PatternContext context)
{
    float2 gridPosition = uv;
    float row = floor(gridPosition.y);
    gridPosition.x += Mod(row, 2.0) * 0.5;
    float2 local = frac(gridPosition) - 0.5;
    float beatPulse = 0.5 + 0.5 * sin(frac(context.beat) * QOOOO_PI * 2.0);
    float radius = lerp(0.2, 0.38, beatPulse);
    float pattern = PatternCircleMask(local, radius);

    return PatternColor(context, pattern);
}


// 5: 下方向へ流れるgrid lines。
float3 FlowingGridPattern(float2 uv, PatternContext context)
{
    float2 position = uv + float2(0.0, context.beat * 0.3);
    float2 local = frac(position);
    float2 distanceToLine = min(local, 1.0 - local);
    float2 lineAA = max(fwidth(position), float2(0.001, 0.001));
    float verticalLine = 1.0 - smoothstep(0.035, 0.035 + lineAA.x, distanceToLine.x);
    float horizontalLine = 1.0 - smoothstep(0.035, 0.035 + lineAA.y, distanceToLine.y);
    float pattern = max(verticalLine, horizontalLine);

    return PatternColor(context, pattern);
}


// 6: 画面中央へ視線を集めるconverging speed lines。
float3 SpeedLinesPattern(float2 uv, PatternContext context)
{
    float2 centered = context.screenUv - 0.5;
    float radius = length(centered);
    float angle = atan2(centered.y, centered.x) + context.beat * 0.035;
    float rayPosition = angle / (QOOOO_PI * 2.0) * 32.0;
    float distanceToRay = abs(frac(rayPosition) - 0.5);
    float rayAA = max(fwidth(rayPosition), 0.001);
    float ray = 1.0 - smoothstep(0.1, 0.1 + rayAA, distanceToRay);
    float center = 1.0 - smoothstep(0.025, 0.13, radius);
    float pattern = max(ray * smoothstep(0.045, 0.12, radius), center);

    return PatternColor(context, pattern);
}


// 7: 列ごとに速度を変えたsquare pixelがbeatに沿って落ちるrain。
float3 PixelRainPattern(float2 uv, PatternContext context)
{
    float column = floor(uv.x * 1.5);
    float speed = lerp(0.35, 1.0, PatternHash(float2(column, 0.0), 53.0));
    float verticalPosition = uv.y * 2.0 + context.beat * speed
                             + PatternHash(float2(column, 1.0), 79.0) * 4.0;
    float2 local = float2(frac(uv.x * 1.5), frac(verticalPosition)) - 0.5;
    float blockDistance = max(abs(local.x), abs(local.y));
    float blockAA = max(fwidth(blockDistance), 0.001);
    float block = 1.0 - smoothstep(0.26, 0.26 + blockAA, blockDistance);
    float cell = floor(verticalPosition);
    float active = step(0.35, PatternHash(float2(column, cell), 101.0));
    float pattern = block * active;

    return PatternColor(context, pattern);
}

#endif// CREATIVE AREA / 創作領域: 各procedural Background patternはUVとPatternContextを受け取る。
// 各functionはPatternDispatcherが選んだstraight RGB colorを返す。
#ifndef QOOOO_PATTERN_BASIC_INCLUDED
#define QOOOO_PATTERN_BASIC_INCLUDED

#include "../Util/Pcg.hlsl"
#include "../Util/MathUtil.hlsl"
#include "PatternCommon.hlsl"


// ==================================================
// Pattern-local helpers
// ==================================================

float PatternHash(float2 position, float seed)
{
    return Pcg01(float3(position, seed)).x;
}


float2 PatternHash2(float2 position, float seed)
{
    return Pcg01(float3(position, seed)).xy;
}


// 補間付きの2D Value Noise。
// Pcg自体はランダム値生成のみなので、連続的なnoiseはこちらで作る。
float PatternValueNoise(float2 position, float seed)
{
    float2 cell = floor(position);
    float2 local = frac(position);

    float2 smoothLocal = local * local * (3.0 - 2.0 * local);

    float bottomLeft = PatternHash(cell + float2(0.0, 0.0), seed);
    float bottomRight = PatternHash(cell + float2(1.0, 0.0), seed);
    float topLeft = PatternHash(cell + float2(0.0, 1.0), seed);
    float topRight = PatternHash(cell + float2(1.0, 1.0), seed);

    float bottom = lerp(bottomLeft, bottomRight, smoothLocal.x);
    float top = lerp(topLeft, topRight, smoothLocal.x);

    return lerp(bottom, top, smoothLocal.y);
}


// 紙、壁、インクなどの自然な階層構造に使う。
// 5 octave固定なのでloop回数はコンパイル時に確定する。
float PatternFbm(float2 position, float seed)
{
    float value = 0.0;
    float amplitude = 0.5;
    float amplitudeSum = 0.0;

    [unroll] for (int i = 0; i < 5; i++)
    {
        value += PatternValueNoise(position, seed + i * 17.0) * amplitude;
        amplitudeSum += amplitude;

        position = position * 2.03 + float2(17.13, 9.71);
        amplitude *= 0.5;
    }

    return value / amplitudeSum;
}


float PatternSignedFbm(float2 position, float seed)
{
    return PatternFbm(position, seed) * 2.0 - 1.0;
}


float PatternCircleMask(float2 position, float radius)
{
    float distanceFromCenter = length(position);
    float aa = max(fwidth(distanceFromCenter), 0.0005);

    return 1.0 - smoothstep(radius - aa, radius + aa, distanceFromCenter);
}


// ==================================================
// 0: Stripe
// ==================================================

float3 StripePattern(float2 uv, PatternContext context)
{
    float linePosition = uv.y * 2.0;
    float lineIndex = floor(linePosition);
    float distanceToCenter = abs(frac(linePosition) - 0.5);
    float lineAA = max(fwidth(linePosition), 0.001);
    float line = 1.0 - smoothstep(0.12, 0.12 + lineAA, distanceToCenter);
    float beatIndex = floor(context.beat);
    float active = step(0.42, PatternHash(float2(lineIndex, beatIndex), 17.0));
    float beatAccent = 1.0 - smoothstep(0.0, 0.28, frac(context.beat));
    float pattern = line * active * lerp(0.58, 1.0, beatAccent);

    return PatternColor(context, pattern);
}


// ==================================================
// 1: Checker
// ==================================================

float3 CheckerPattern(float2 uv, PatternContext context)
{
    float2 position = uv;
    position.y += context.beat * 0.25;
    float pattern = Mod(floor(position.x) + floor(position.y), 2.0);

    return PatternColor(context, pattern);
}


// ==================================================
// 2: Wave
// ==================================================

float3 WavePattern(float2 uv, PatternContext context)
{
    float movingX = uv.x - context.beat * 0.5;
    float waveCenter = 0.5 + sin(movingX * QOOOO_PI * 2.0) * 0.28;
    float distanceToWave = abs(frac(uv.y) - waveCenter);
    float waveAA = max(fwidth(distanceToWave), 0.001);
    float pattern = 1.0 - smoothstep(0.035, 0.035 + waveAA, distanceToWave);

    return PatternColor(context, pattern);
}


// ==================================================
// 3: Infinite Rings
// ==================================================

float3 InfiniteRingsPattern(float2 uv, PatternContext context)
{
    float radius = length(context.screenUv - 0.5);
    float ringPosition = radius * 18.0 - context.beat * 1.5;
    float distanceToRing = abs(frac(ringPosition) - 0.5);
    float ringAA = max(fwidth(ringPosition), 0.001);
    float pattern = 1.0 - smoothstep(0.055, 0.055 + ringAA, distanceToRing);

    return PatternColor(context, pattern);
}


// ==================================================
// 4: Polka Dot
// ==================================================

float3 BeatPolkaDotPattern(float2 uv, PatternContext context)
{
    float2 gridPosition = uv;
    float row = floor(gridPosition.y);
    gridPosition.x += Mod(row, 2.0) * 0.5;
    float2 local = frac(gridPosition) - 0.5;
    float beatPulse = 0.5 + 0.5 * sin(frac(context.beat) * QOOOO_PI * 2.0);
    float radius = lerp(0.2, 0.38, beatPulse);
    float pattern = PatternCircleMask(local, radius);

    return PatternColor(context, pattern);
}


// ==================================================
// 5: Flowing Grid
// ==================================================

float3 FlowingGridPattern(float2 uv, PatternContext context)
{
    float2 position = uv + float2(0.0, context.beat * 0.3);
    float2 local = frac(position);
    float2 distanceToLine = min(local, 1.0 - local);
    float2 lineAA = max(fwidth(position), float2(0.001, 0.001));
    float verticalLine = 1.0 - smoothstep(0.035, 0.035 + lineAA.x, distanceToLine.x);
    float horizontalLine = 1.0 - smoothstep(0.035, 0.035 + lineAA.y, distanceToLine.y);
    float pattern = max(verticalLine, horizontalLine);

    return PatternColor(context, pattern);
}


// ==================================================
// 6: Speed Lines
// ==================================================

float3 SpeedLinesPattern(float2 uv, PatternContext context)
{
    float2 centered = context.screenUv - 0.5;
    float radius = length(centered);
    float angle = atan2(centered.y, centered.x) + context.beat * 0.035;
    float rayPosition = angle / (QOOOO_PI * 2.0) * 32.0;
    float distanceToRay = abs(frac(rayPosition) - 0.5);
    float rayAA = max(fwidth(rayPosition), 0.001);
    float ray = 1.0 - smoothstep(0.1, 0.1 + rayAA, distanceToRay);
    float center = 1.0 - smoothstep(0.025, 0.13, radius);
    float pattern = max(ray * smoothstep(0.045, 0.12, radius), center);

    return PatternColor(context, pattern);
}


// ==================================================
// 7: Pixel Rain
// ==================================================

float3 PixelRainPattern(float2 uv, PatternContext context)
{
    float column = floor(uv.x * 1.5);
    float speed = lerp(0.35, 1.0, PatternHash(float2(column, 0.0), 53.0));
    float verticalPosition = uv.y * 2.0 + context.beat * speed
                             + PatternHash(float2(column, 1.0), 79.0) * 4.0;
    float2 local = float2(frac(uv.x * 1.5), frac(verticalPosition)) - 0.5;
    float blockDistance = max(abs(local.x), abs(local.y));
    float blockAA = max(fwidth(blockDistance), 0.001);
    float block = 1.0 - smoothstep(0.26, 0.26 + blockAA, blockDistance);
    float cell = floor(verticalPosition);
    float active = step(0.35, PatternHash(float2(column, cell), 101.0));
    float pattern = block * active;

    return PatternColor(context, pattern);
}


// ==================================================
// 8: Grid Lines
// ==================================================

float3 GridLinePattern(float2 uv, PatternContext context)
{
    float2 gridCount = float2(16.0, 9.0);

    float2 gridPosition = uv * gridCount;
    float2 gridUV = frac(gridPosition);

    float2 distanceToLine = min(gridUV, 1.0 - gridUV);

    float lineWidth = 0.025;

    float verticalAA = max(fwidth(gridPosition.x), 0.001);
    float horizontalAA = max(fwidth(gridPosition.y), 0.001);

    float verticalLine = 1.0 - smoothstep(lineWidth, lineWidth + verticalAA, distanceToLine.x);

    float horizontalLine = 1.0 - smoothstep(lineWidth, lineWidth + horizontalAA, distanceToLine.y);

    float pattern = max(verticalLine, horizontalLine);

    return PatternColor(context, pattern);
}


// ==================================================
// 9: Psychedelic Rings
// ==================================================

float3 PsychedelicRingPattern(float2 uv, PatternContext context)
{
    // アスペクト補正はShader本体側で既に実施済み
    float2 centeredUV = uv - 0.5;

    float angle = atan2(centeredUV.y, centeredUV.x);
    float radius = length(centeredUV);

    float waveFrequency = 8.0;
    float waveAmplitude = 0.08;

    float wave = sin(angle * waveFrequency) * waveAmplitude;
    float distortedRadius = radius + wave;

    float ringCount = 12.0;
    float expansion = context.time * 0.5;

    float pattern = Mod(floor((distortedRadius - expansion) * ringCount), 2.0);

    return PatternColor(context, pattern);
}


// ==================================================
// 10: Hand Drawn Grid
// ==================================================
//
// 参考画像のような、手描きされた微妙に歪んだ格子。
// 各線そのものをrandomにするのではなく、
// 連続noiseで座標系を歪めることで線の連続性を維持する。

float3 HandDrawnGridPattern(float2 uv, PatternContext context)
{
    float2 gridCount = float2(8.0, 12.0);

    float2 position = uv * gridCount;

    float horizontalWarp = PatternSignedFbm(float2(position.y * 0.32, 4.1), 11.0) * 0.18;

    float verticalWarp = PatternSignedFbm(float2(position.x * 0.32, 8.7), 29.0) * 0.18;

    position += float2(horizontalWarp, verticalWarp);

    float2 local = frac(position);
    float2 distanceToLine = min(local, 1.0 - local);

    float lineWidth = 0.025;

    float verticalAA = max(fwidth(position.x), 0.001);
    float horizontalAA = max(fwidth(position.y), 0.001);

    float verticalLine = 1.0 - smoothstep(lineWidth, lineWidth + verticalAA, distanceToLine.x);

    float horizontalLine = 1.0 - smoothstep(lineWidth, lineWidth + horizontalAA, distanceToLine.y);

    float pattern = max(verticalLine, horizontalLine);

    return PatternColor(context, pattern);
}


// ==================================================
// 11: Organic Dots
// ==================================================
//
// 完全な円ではなく、セルごとに形状と配置が微妙に異なる。
// 紙を切ったようなorganicなドット。

float3 OrganicDotPattern(float2 uv, PatternContext context)
{
    float2 gridCount = float2(5.0, 7.0);

    float2 gridPosition = uv * gridCount;

    float row = floor(gridPosition.y);
    gridPosition.x += Mod(row, 2.0) * 0.15;

    float2 cell = floor(gridPosition);
    float2 local = frac(gridPosition) - 0.5;

    float2 positionJitter = (PatternHash2(cell, 13.0) - 0.5) * 0.10;

    local -= positionJitter;

    float randomRadius = PatternHash(cell, 31.0);
    float phaseA = PatternHash(cell, 47.0) * QOOOO_PI * 2.0;
    float phaseB = PatternHash(cell, 71.0) * QOOOO_PI * 2.0;

    float angle = atan2(local.y, local.x);

    float radius = 0.36 + (randomRadius - 0.5) * 0.07 + sin(angle * 5.0 + phaseA) * 0.025 + sin(angle * 9.0 + phaseB) *
        0.012;

    float distanceFromCenter = length(local);
    float aa = max(fwidth(distanceFromCenter), 0.001);

    float pattern = 1.0 - smoothstep(radius - aa, radius + aa, distanceFromCenter);

    return PatternColor(context, pattern);
}


// ==================================================
// 12: Halftone Circles
// ==================================================
//
// 大きいcircleと、少しoffsetした横線halftoneを重ねる。
// 2色Contextなので中間値を使って疑似3階調にする。

float3 HalftoneCirclePattern(float2 uv, PatternContext context)
{
    float2 gridCount = float2(4.0, 5.0);

    float2 gridPosition = uv * gridCount;

    float row = floor(gridPosition.y);
    gridPosition.x += Mod(row, 2.0) * 0.5;

    float2 local = frac(gridPosition) - 0.5;

    float baseCircle = PatternCircleMask(local, 0.36);

    float2 halftonePosition = local + float2(0.12, -0.06);

    float halftoneCircle = PatternCircleMask(halftonePosition, 0.36);

    float lineCount = 20.0;

    float linePosition = abs(frac((halftonePosition.y + 0.5) * lineCount) - 0.5);

    float lineAA = max(fwidth(halftonePosition.y * lineCount), 0.001);

    float horizontalLine = 1.0 - smoothstep(0.06, 0.06 + lineAA, linePosition);

    float dashPosition = frac((halftonePosition.x + 0.5) * 10.0);

    float dash = 1.0 - smoothstep(0.68, 0.72, dashPosition);

    float halftone = halftoneCircle * horizontalLine * dash;

    float pattern = saturate(baseCircle * 0.72 + halftone * 0.48);

    return PatternColor(context, pattern);
}


// ==================================================
// 13: Paper Grain
// ==================================================
//
// 滑らかな低周波noise + 中周波noise + 細粒noise。
// Main/Subを近い色にすると紙テクスチャとして使いやすい。

float3 PaperGrainPattern(float2 uv, PatternContext context)
{
    float largeGrain = PatternFbm(uv * 5.0, 11.0);
    float mediumGrain = PatternFbm(uv * 28.0, 31.0);
    float fineGrain = PatternValueNoise(uv * 180.0, 67.0);

    // 紙繊維っぽい縦長成分
    float fiber = PatternValueNoise(float2(uv.x * 30.0, uv.y * 260.0), 101.0);

    float pattern = 0.5 + (largeGrain - 0.5) * 0.30 + (mediumGrain - 0.5) * 0.18 + (fineGrain - 0.5) * 0.08 + (fiber -
        0.5) * 0.05;

    return PatternColor(context, saturate(pattern));
}


// ==================================================
// 14: Plaster / Wall
// ==================================================
//
// 漆喰や古い壁紙のようなmacroなムラ。
// Paper Grainより低周波成分が強い。

float3 PlasterPattern(float2 uv, PatternContext context)
{
    float large = PatternFbm(uv * 2.5, 13.0);
    float medium = PatternFbm(uv * 14.0, 41.0);
    float fine = PatternFbm(uv * 70.0, 79.0);

    float poreNoise = PatternValueNoise(uv * 150.0, 131.0);

    float pores = smoothstep(0.84, 0.96, poreNoise);

    float ridges = abs(PatternSignedFbm(uv * 7.0, 157.0));

    float pattern = 0.55 + (large - 0.5) * 0.32 + (medium - 0.5) * 0.16 + (fine - 0.5) * 0.07 + (ridges - 0.5) * 0.05 -
        pores * 0.12;

    return PatternColor(context, saturate(pattern));
}


// ==================================================
// 15: Ink Speckle
// ==================================================
//
// インクの抜け、紙への定着ムラ、細かな飛沫。
// 単体でも使えるが他patternへのfinishとして特に有効。

float3 InkSpecklePattern(float2 uv, PatternContext context)
{
    float macro = PatternFbm(uv * 7.0, 17.0);
    float grain = PatternValueNoise(uv * 220.0, 83.0);

    float threshold = lerp(0.78, 0.94, macro);

    float fineSpeckles = smoothstep(threshold, threshold + 0.04, grain);

    float largeNoise = PatternValueNoise(uv * 50.0, 109.0);

    float largeSpeckles = smoothstep(0.91, 0.97, largeNoise);

    float pattern = saturate(fineSpeckles + largeSpeckles * 0.7);

    return PatternColor(context, pattern);
}


// ==================================================
// 16: Wavy Checker
// ==================================================
//
// 整然としたcheckerではなく、
// 手描き・布・印刷ズレに近い歪みを持つchecker。

float3 WavyCheckerPattern(float2 uv, PatternContext context)
{
    float2 position = uv * float2(8.0, 6.0);

    float waveX = sin(position.y * 1.8) * 0.10;

    float waveY = sin(position.x * 1.6 + 1.3) * 0.10;

    float noiseX = PatternSignedFbm(position * 0.22, 31.0) * 0.10;

    float noiseY = PatternSignedFbm(position * 0.22 + 13.7, 57.0) * 0.10;

    position += float2(waveX + noiseX, waveY + noiseY);

    float pattern = Mod(floor(position.x) + floor(position.y), 2.0);

    return PatternColor(context, pattern);
}


// ==================================================
// 17: Linen / Fabric
// ==================================================
//
// 縦横の繊維と細かなgrain。
// Main/Subを近い色にすることで布やキャンバスに見せる。

float3 LinenPattern(float2 uv, PatternContext context)
{
    float2 threadPosition = uv * 180.0;

    float verticalThread = pow(abs(sin(threadPosition.x * QOOOO_PI)), 14.0);

    float horizontalThread = pow(abs(sin(threadPosition.y * QOOOO_PI)), 14.0);

    float unevenness = PatternFbm(uv * 45.0, 43.0);

    float fineGrain = PatternValueNoise(uv * 240.0, 73.0);

    float threads = (verticalThread + horizontalThread) * 0.22;

    float pattern = 0.42 + threads + (unevenness - 0.5) * 0.12 + (fineGrain - 0.5) * 0.06;

    return PatternColor(context, saturate(pattern));
}


// ==================================================
// 18: Marble Paper
// ==================================================
//
// fBmによるdomain warpを入れたマーブル模様。
// ある程度ゆっくり動かしてもよいが、
// 背景素材として静的になるようtimeは使わない。

float3 MarblePattern(float2 uv, PatternContext context)
{
    float2 position = uv * 3.5;

    float warpA = PatternFbm(position, 19.0);

    float warpB = PatternFbm(position + float2(7.3, 11.1), 43.0);

    float2 warp = float2(warpA, warpB) - 0.5;

    float detail = PatternFbm(position + warp * 2.5, 67.0);

    float marbleCoordinate = uv.x * 7.0 + uv.y * 1.2 + detail * 2.5;

    float bands = sin(marbleCoordinate * QOOOO_PI);

    float pattern = smoothstep(-0.35, 0.35, bands);

    return PatternColor(context, pattern);
}


// ==================================================
// 19: Rough Stripes
// ==================================================
//
// シルクスクリーンや古い印刷のようなstripe。
// 輪郭を連続noiseで歪め、内部にはink dropoutを作る。

float3 RoughStripePattern(float2 uv, PatternContext context)
{
    float lineCount = 12.0;

    float distortion = PatternSignedFbm(float2(uv.y * 4.0, 2.1), 23.0) * 0.055;

    float movement = context.beat * 0.02;

    float stripePosition = (uv.x + distortion + movement) * lineCount;

    float stripe = Mod(floor(stripePosition), 2.0);

    float inkDensity = PatternFbm(uv * 70.0, 71.0);

    float fineInk = PatternValueNoise(uv * 210.0, 113.0);

    float coverage = smoothstep(0.18, 0.44, inkDensity) * smoothstep(0.08, 0.30, fineInk);

    float pattern = stripe * coverage;

    return PatternColor(context, pattern);
}

#endif
