#ifndef QOOOO_PATTERN_MATH_INCLUDED
#define QOOOO_PATTERN_MATH_INCLUDED


#ifndef QOOOO_PI
#define QOOOO_PI 3.14159265358979
#endif


float Mod(float x, float y)
{
    return x - y * floor(x / y);
}


float2 Rotate2D(
    float2 position,
    float angle
)
{
    float s = sin(angle);
    float c = cos(angle);

    return float2(
        c * position.x
        - s * position.y,

        s * position.x
        + c * position.y
    );
}


float2 XyToPolar(float2 xy)
{
    return float2(
        atan2(xy.y, xy.x),
        length(xy)
    );
}


float2 PolarToXy(float2 polar)
{
    return polar.y
        * float2(
            cos(polar.x),
            sin(polar.x)
        );
}


float Gray(float3 color)
{
    return dot(
        color,
        float3(
            0.299,
            0.587,
            0.114
        )
    );
}


float Map(
    float value,
    float min1,
    float max1,
    float min2,
    float max2
)
{
    return min2
        + (value - min1)
        * (max2 - min2)
        / (max1 - min1);
}


float Zigzag(float x)
{
    return abs(
        Mod(x, 2.0) - 1.0
    );
}

float2 ApplyAspectCorrection(
    float2 uv,
    float2 resolution
)
{
    resolution =
        max(
            resolution,
            float2(1.0, 1.0)
        );

    uv -= 0.5;

    uv.y *=
        resolution.y
        / resolution.x;

    uv += 0.5;

    return uv;
}

#endif
