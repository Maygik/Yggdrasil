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
    float IsSelected;
    float IsHovered;
    float Padding1;
};

cbuffer PerMaterialConstants : register(b2)
{
    float3 Tint;
    float HasBaseTexture;
    float HasNormalMap;
    float3 Padding2;
};

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
    float3 Bitangent : BINORMAL;
    float2 TexCoord : TEXCOORD0;
    int4 BoneIndices : BLENDINDICES;
    float4 BoneWeights : BLENDWEIGHT;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : TEXCOORD0;
    float3 WorldPosition : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
};

VSOutput VSMain(VSInput input)
{
    VSOutput output;

    float4 worldPosition = mul(float4(input.Position, 1.0f), World);
    float3 worldNormal = normalize(mul(float4(input.Normal, 0.0f), World).xyz);

    output.Position = mul(worldPosition, ViewProjection);
    output.WorldNormal = worldNormal;
    output.WorldPosition = worldPosition.xyz;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    float3 normal = normalize(input.WorldNormal);
    float3 lightDirection = normalize(-LightDirection);
    float lambert = saturate(dot(normal, lightDirection));
    float lighting = saturate(AmbientStrength + ((1.0f - AmbientStrength) * lambert));

    float3 baseColor = Tint;
    float3 litColor = baseColor * lighting;

    float3 hoverColor = float3(0.28f, 0.70f, 1.00f);
    float3 selectedColor = float3(1.00f, 0.75f, 0.20f);
    float3 overlayColor = lerp(hoverColor, selectedColor, saturate(IsSelected));
    float3 finalColor = lerp(litColor, overlayColor, saturate(HighlightMix));

    return float4(finalColor, 1.0f);
}
