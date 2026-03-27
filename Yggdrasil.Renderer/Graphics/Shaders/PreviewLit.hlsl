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

cbuffer PerMaterialConstants : register(b2)
{
    float3 Tint;
    float HasBaseTexture;
    float HasNormalTexture;
    float HasEmissiveTexture;
    float HasLightWarpTexture;
    float HasEnvMapTexture;
    float HasEnvMapMaskTexture;
    float HasPhongExponentTexture;
    float HasPhongMaskTexture;
    float NoTint;
    float AlphaTest;
    float AlphaTestReference;
    float AllowAlphaToCoverage;
    float NoCull;
    float Translucent;
    float Additive;
    float HalfLambert;
    float SelfIllum;
    float EmissiveBlendStrength;
    float UseEnvMapProbes;
    float EnvMapContrast;
    float Phong;
    float3 EnvMapTint;
    float RimLight;
    float PhongBoost;
    float PhongExponent;
    float RimLightExponent;
    float RimLightBoost;
    float3 PhongFresnelRanges;
    float Adjusted;
    float3 PhongTint;
    float MaterialPadding0;
    float3 RimLightTint;
    float MaterialPadding1;
};

Texture2D BaseTextureMap : register(t0);
Texture2D NormalTextureMap : register(t1);
Texture2D EmissiveTextureMap : register(t2);
Texture2D LightWarpTextureMap : register(t3);
Texture2D EnvMapTextureMap : register(t4);
Texture2D EnvMapMaskTextureMap : register(t5);
Texture2D PhongExponentTextureMap : register(t6);
Texture2D PhongMaskTextureMap : register(t7);
SamplerState MaterialSampler : register(s0);

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
    output.TexCoord = float2(input.TexCoord.x, -input.TexCoord.y); // Flip the V coordinate for DirectX
    return output;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    float3 normal = normalize(input.WorldNormal);
    float3 lightDirection = normalize(-LightDirection);
    float lambert = saturate(dot(normal, lightDirection));
    float lighting = saturate(AmbientStrength + ((1.0f - AmbientStrength) * lambert * 0.5));

    float4 baseSample = HasBaseTexture > 0.5f
        ? BaseTextureMap.Sample(MaterialSampler, input.TexCoord)
        : float4(1.0f, 1.0f, 1.0f, 1.0f);

    if (AlphaTest > 0.5f && baseSample.a < AlphaTestReference)
    {
        discard;
    }

    if (SelfIllum > 0.5f)
    {
        lighting = 1.0f;
    }

    float3 baseColor = baseSample.rgb * Tint;
    float3 litColor = baseColor * lighting;

    float3 hoverColor = float3(0.28f, 0.70f, 1.00f);
    float3 finalColor = lerp(litColor, hoverColor, saturate(HighlightMix));

    return float4(finalColor, baseSample.a);
}
