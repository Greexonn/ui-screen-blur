#ifndef UNITY_FILTERING_INCLUDED
#define UNITY_FILTERING_INCLUDED

#define TEXTURE2D_ARGS(textureName, samplerName)                textureName, samplerName
#define TEXTURE2D(textureName)                Texture2D textureName
#define SAMPLER(samplerName)                  SamplerState samplerName
#define TEXTURE2D_PARAM(textureName, samplerName)                 TEXTURE2D(textureName),         SAMPLER(samplerName)
#define PLATFORM_SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)                      textureName.SampleLevel(samplerName, coord2, lod)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)                      PLATFORM_SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)

// Basic B-Spline of the 3nd degree (4th order, support = 4).
// The fractional coordinate of each part is assumed to be in the [0, 1] range.
// https://www.desmos.com/calculator/479pgatwlt
//
// Sample use-case:
// float2 xy = uv * resolution.xy;
// float2 ic = round(xy) + 0.5; // Cell-centered (dual grid)
// float2 fc = ic - xy;         // Inverse-translate the the filter around 0.5 with a wrap
// Then pass x = fc.
//
half2 BSpline3Leftmost(half2 x)
{
    return 0.16666667 * x * x * x;
}

half2 BSpline3MiddleLeft(half2 x)
{
    return 0.16666667 + x * (0.5 + x * (0.5 - x * 0.5));
}

half2 BSpline3MiddleRight(half2 x)
{
    return 0.66666667 + x * (-1.0 + 0.5 * x) * x;
}

half2 BSpline3Rightmost(half2 x)
{
    return 0.16666667 + x * (-0.5 + x * (0.5 - x * 0.16666667));
}

// Compute weights & offsets for 4x bilinear taps for the bicubic B-Spline filter.
// The fractional coordinate should be in the [0, 1] range (centered on 0.5).
// Inspired by: http://vec3.ca/bicubic-filtering-in-fewer-taps/
void BicubicFilter(float2 fracCoord, out float2 weights[2], out float2 offsets[2])
{
    float2 r  = BSpline3Rightmost(fracCoord);
    float2 mr = BSpline3MiddleRight(fracCoord);
    float2 ml = BSpline3MiddleLeft(fracCoord);
    float2 l  = 1.0 - mr - ml - r;

    weights[0] = r + mr;
    weights[1] = ml + l;
    offsets[0] = -1.0 + mr * rcp(weights[0]);
    offsets[1] =  1.0 + l * rcp(weights[1]);
}

// texSize = (width, height, 1/width, 1/height)
float4 SampleTexture2DBicubic(TEXTURE2D_PARAM(tex, smp), float2 coord, float4 texSize, float2 maxCoord, uint unused /* needed to match signature of texarray version below */)
{
    float2 xy = coord * texSize.xy + 0.5;
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    float2 weights[2], offsets[2];
    BicubicFilter(fc, weights, offsets);

    return weights[0].y * (weights[0].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[0].y) - 0.5) * texSize.zw, maxCoord), 0.0)  +
                           weights[1].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[0].y) - 0.5) * texSize.zw, maxCoord), 0.0)) +
           weights[1].y * (weights[0].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[1].y) - 0.5) * texSize.zw, maxCoord), 0.0)  +
                           weights[1].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[1].y) - 0.5) * texSize.zw, maxCoord), 0.0));
}

#endif 