#ifndef QOOOO_PATTERN_BASIC_INCLUDED
#define QOOOO_PATTERN_BASIC_INCLUDED

#include "../Util/Pcg.hlsl"
#include "../Util/PatternMath.hlsl"
#include "PatternCommon.hlsl"


// ==================================================
// 0: Paper Noise
// ==================================================

float3 NoiseTexturePattern(
    float2 uv,
    PatternContext context
)
{
    float noise = 0.0;

    noise +=
        PcgRandom(
            floor(uv * 8.0),
            8u
        )
        * 0.30;

    noise +=
        PcgRandom(
            floor(uv * 20.0),
            20u
        )
        * 0.25;

    noise +=
        PcgRandom(
            floor(uv * 50.0),
            50u
        )
        * 0.20;

    noise +=
        PcgRandom(
            floor(uv * 100.0),
            100u
        )
        * 0.15;

    noise +=
        PcgRandom(
            floor(uv * 200.0),
            200u
        )
        * 0.10;

    float blend =
        smoothstep(
            0.3,
            0.7,
            noise
        );

    return PatternColor(
        context,
        blend
    );
}


// ==================================================
// 1: Diagonal Stripes
// ==================================================

float3 DiagonalStripePattern(
    float2 uv,
    PatternContext context
)
{
    float2 rotatedUV =
        Rotate2D(
            uv,
            QOOOO_PI / 4.0
        );

    float lineCount =
        10.0;

    float movement =
        context.beat
        * 0.05;

    float pattern =
        Mod(
            floor(
                (
                    rotatedUV.y
                    + movement
                )
                * lineCount
            ),
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 2: Vertical Stripes
// ==================================================

float3 VerticalStripePattern(
    float2 uv,
    PatternContext context
)
{
    float lineCount =
        6.67;

    float movement =
        context.beat
        * 0.05;

    float pattern =
        Mod(
            floor(
                (
                    uv.x
                    + movement
                )
                * lineCount
            ),
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 3: Horizontal Stripes
// ==================================================

float3 HorizontalStripePattern(
    float2 uv,
    PatternContext context
)
{
    float lineCount =
        33.33;

    float movement =
        context.beat
        * 0.05;

    float pattern =
        Mod(
            floor(
                (
                    uv.y
                    + movement
                )
                * lineCount
            ),
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 4: Wave Stripes
// ==================================================

float3 WaveStripePattern(
    float2 uv,
    PatternContext context
)
{
    float lineCount =
        14.29;

    float wave =
        sin(
            uv.y
            * QOOOO_PI
            * 4.0
            + context.time
            * 2.0
        )
        * 0.3;

    float pattern =
        Mod(
            floor(
                (
                    uv.x
                    + wave
                )
                * lineCount
            ),
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 5: Checkerboard
// ==================================================

float3 CheckerboardPattern(
    float2 uv,
    PatternContext context
)
{
    float gridCountX =
        8.0;

    float gridCountY =
        5.0;

    float cellX =
        floor(
            uv.x
            * gridCountX
        );

    float cellY =
        floor(
            uv.y
            * gridCountY
        );

    float basePattern =
        Mod(
            cellX
            + cellY,
            2.0
        );

    float beatSwap =
        Mod(
            floor(
                context.beat
            ),
            2.0
        );

    float pattern =
        Mod(
            basePattern
            + beatSwap,
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 6: Polka Dot
// ==================================================

float3 PolkaDotPattern(
    float2 uv,
    PatternContext context
)
{
    float2 gridCount =
        float2(
            8.0,
            5.0
        );

    float2 gridPosition =
        uv * gridCount;

    float row =
        floor(
            gridPosition.y
        );

    // 奇数行を半セルずらす
    float rowOffset =
        Mod(row, 2.0)
        * 0.5;

    gridPosition.x +=
        rowOffset;

    float2 localUV =
        frac(
            gridPosition
        );

    float distanceFromCenter =
        length(
            localUV
            - float2(
                0.5,
                0.5
            )
        );

    float radius =
        0.3;

    float pattern =
        step(
            distanceFromCenter,
            radius
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 7: Sunburst
// ==================================================

float3 SunburstPattern(
    float2 uv,
    PatternContext context
)
{
    float2 centeredUV =
        uv - 0.5;

    float angle =
        atan2(
            centeredUV.y,
            centeredUV.x
        );

    float rayCount =
        12.0;

    float rotation =
        context.beat
        * 0.1;

    float pattern =
        Mod(
            floor(
                (
                    angle
                    + rotation
                )
                / QOOOO_PI
                * rayCount
            ),
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 8: Grid Lines
// ==================================================

float3 GridLinePattern(
    float2 uv,
    PatternContext context
)
{
    float2 gridCount =
        float2(
            16.0,
            9.0
        );

    float2 gridUV =
        frac(
            uv
            * gridCount
        );

    float lineWidth =
        0.02;

    float verticalDistance =
        abs(
            gridUV.x
            - 0.5
        );

    float horizontalDistance =
        abs(
            gridUV.y
            - 0.5
        );

    float verticalAA =
        max(
            fwidth(gridUV.x),
            0.001
        );

    float horizontalAA =
        max(
            fwidth(gridUV.y),
            0.001
        );

    float verticalLine =
        1.0
        - smoothstep(
            lineWidth,
            lineWidth
            + verticalAA,
            verticalDistance
        );

    float horizontalLine =
        1.0
        - smoothstep(
            lineWidth,
            lineWidth
            + horizontalAA,
            horizontalDistance
        );

    float pattern =
        max(
            verticalLine,
            horizontalLine
        );

    return PatternColor(
        context,
        pattern
    );
}


// ==================================================
// 9: Psychedelic Rings
// ==================================================

float3 PsychedelicRingPattern(
    float2 uv,
    PatternContext context
)
{
    // アスペクト補正はShader本体側で
    // 既に実施済み
    float2 centeredUV =
        uv - 0.5;

    float angle =
        atan2(
            centeredUV.y,
            centeredUV.x
        );

    float radius =
        length(
            centeredUV
        );

    float waveFrequency =
        8.0;

    float waveAmplitude =
        0.08;

    float wave =
        sin(
            angle
            * waveFrequency
        )
        * waveAmplitude;

    float distortedRadius =
        radius
        + wave;

    float ringCount =
        12.0;

    float expansion =
        context.time
        * 0.5;

    float pattern =
        Mod(
            floor(
                (
                    distortedRadius
                    - expansion
                )
                * ringCount
            ),
            2.0
        );

    return PatternColor(
        context,
        pattern
    );
}

#endif
