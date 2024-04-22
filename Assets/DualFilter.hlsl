#ifndef DUAL_FILTER_INCLUDED
#define DUAL_FILTER_INCLUDED

#include "Filter.hlsl"

float4 _ProjectionParams;

TEXTURE2D(_BlurSource);
TEXTURE2D(_BlurSource2);
float4 _BlurSource_TexelSize;
float4 _BlurSource2_TexelSize;

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

SAMPLER(sampler_linear_clamp);

float4 GetSourceTexelSize ()
{
    return _BlurSource_TexelSize;
}

float4 GetSource (const float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_BlurSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2 (const float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_BlurSource2, sampler_linear_clamp, screenUV, 0);
}

Varyings DefaultPassVertex (const uint vertexID : SV_VertexID)
{
    Varyings output;
    output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);

    if (_ProjectionParams.x < 0.0)
    {
        output.screenUV.y = 1.0 - output.screenUV.y;
    }
    
    return output;
}

float4 CopyPassFragment (Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}

float4 BlurDualFilterDownSamplePassFragment(const Varyings input) : SV_TARGET
{
    float2 uv = input.screenUV;
    const float2 offset = GetSourceTexelSize();
    
    float4 color = GetSource(uv) * 4.0;

    uv = input.screenUV - offset;
    color += GetSource(uv);

    uv = input.screenUV + offset;
    color += GetSource(uv);

    uv = float2(input.screenUV.x + offset.x, input.screenUV.y - offset.y);
    color += GetSource(uv);

    uv = float2(input.screenUV.x - offset.x, input.screenUV.y + offset.y);
    color += GetSource(uv);

    return color * 0.125; // sum / 8.0
}

float4 BlurDualFilterUpSamplePassFragment(const Varyings input) : SV_TARGET
{
    const float4 source = GetSource2(input.screenUV);
    const float2 offset = GetSourceTexelSize();

    float2 uv = float2(input.screenUV.x - offset.x * 2.0, input.screenUV.y);
    float4 color = GetSource(uv);

    uv = float2(input.screenUV.x - offset.x, input.screenUV.y + offset.y);
    color += GetSource(uv) * 2.0;

    uv = float2(input.screenUV.x, input.screenUV.y + offset.y * 2.0);
    color += GetSource(uv);

    uv = float2(input.screenUV.x + offset.x, input.screenUV.y + offset.y);
    color += GetSource(uv) * 2.0;

    uv = float2(input.screenUV.x + offset.x * 2.0, input.screenUV.y);
    color += GetSource(uv);

    uv = float2(input.screenUV.x + offset.x, input.screenUV.y - offset.y);
    color += GetSource(uv) * 2.0;

    uv = float2(input.screenUV.x, input.screenUV.y - offset.y * 2.0);
    color += GetSource(uv);

    uv = float2(input.screenUV.x - offset.x, input.screenUV.y - offset.y);
    color += GetSource(uv) * 2.0;

    color = color * 1.0 / 12.0;

    return color;
}

#endif
