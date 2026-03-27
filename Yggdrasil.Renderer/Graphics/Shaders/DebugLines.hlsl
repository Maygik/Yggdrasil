cbuffer PerFrameConstants : register(b0)
{
    row_major float4x4 ViewProjection;
    float3 CameraPosition;
    float Padding0;
    float3 LightDirection;
    float AmbientStrength;
};

cbuffer PerObjectConstants : register(b1)
{
    row_major float4x4 World;
    float HighlightMix;
    float IsHovered;
    float ObjectPadding0;
    float ObjectPadding1;
};

struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

VSOutput VSMain(VSInput input)
{
    VSOutput output;
    float4 worldPosition = mul(float4(input.Position, 1.0f), World);
    output.Position = mul(worldPosition, ViewProjection);
    output.Color = input.Color;
    return output;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    return input.Color;
}
