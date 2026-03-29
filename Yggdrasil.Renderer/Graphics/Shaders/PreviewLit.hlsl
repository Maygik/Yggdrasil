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
    float HasBaseTexture;                       // Used
    float HasNormalTexture;                     // Used
    float HasEmissiveTexture;                   // Used
    float HasLightWarpTexture;                  // 
    float HasEnvMapTexture;                     // 
    float HasEnvMapMaskTexture;                 // 
    float HasPhongExponentTexture;              // 
    float HasPhongMaskTexture;                  // 
    float NoTint;                               // Used
    float AlphaTest;                            // Used
    float AlphaTestReference;                   // Used
    float AllowAlphaToCoverage;                 // Not planned, too annoying, GPUs sort of do it well enough
    float NoCull;                               // Not a shader thing: rasteriser thing
    float Translucent;                          // More complex
    float Additive;                             // More complex
    float HalfLambert;                          // Used
    float SelfIllum;                            // Used
    float EmissiveBlendStrength;                // Used
    float UseEnvMapProbes;                      // 
    float EnvMapContrast;                       // 
    float Phong;                                // 
    float3 EnvMapTint;                          // 
    float RimLight;                             // 
    float PhongBoost;                           // 
    float PhongExponent;                        // 
    float RimLightExponent;                     // 
    float RimLightBoost;                        // 
    float3 PhongFresnelRanges;                  // 
    float Adjusted;                             // 
    float3 PhongTint;                           // 
    float MaterialPadding0;                     // 
    float3 RimLightTint;                        // 
    float MaterialPadding1;                     // 
};

Texture2D BaseTextureMap : register(t0);        // Used
Texture2D NormalTextureMap : register(t1);      // Used
Texture2D EmissiveTextureMap : register(t2);    // Used
Texture2D LightWarpTextureMap : register(t3);   // 
Texture2D EnvMapTextureMap : register(t4);      // 
Texture2D EnvMapMaskTextureMap : register(t5);  // 
Texture2D PhongExponentTextureMap : register(t6);   //
Texture2D PhongMaskTextureMap : register(t7);   // 
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
    float3 Tangent : TANGENT;
    float3 Bitangent: BINORMAL;
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




// https://developer.valvesoftware.com/wiki/Phong_materials#Phong_Fresnel_ranges
float Fresnel2( float3 vNormal, float3 vEyeDir )
{
	float fresnel = 1-saturate( dot( vNormal, vEyeDir ) );				// 1-(N.V) for Fresnel term
	return fresnel * fresnel;											// Square for a more subtle look
}

float Fresnel(float3 vNormal, float3 vEyeDir, float3 vRanges)
{

	float result, f = Fresnel2( vNormal, vEyeDir );			// Traditional Fresnel

	if ( f > 0.5f )
		result = lerp( vRanges.y, vRanges.z, (2*f)-1 );		// Blend between mid and high values
	else
        result = lerp( vRanges.x, vRanges.y, 2*f );			// Blend between low and mid values

	return result;
}




float4 PSMain(VSOutput input) : SV_TARGET
{
    // Normalize the normal and light direction
    float3 normal = normalize(input.WorldNormal);
    float3 lightDirection = normalize(-LightDirection);


    // Use the normal map
    float3 normalSample = HasNormalTexture > 0.5f
        ? NormalTextureMap.Sample(MaterialSampler, input.TexCoord).rgb
        : float3(0.5f, 0.5f, 1.0f);
    
    // Transform the normal from [0, 1] range to [-1, 1] range
    normalSample = (normalSample * 2.0f) - float3(1.0f, 1.0f, 1.0f);

    // Reconstruct the normal in world space using the tangent, bitangent, and normal from the vertex shader
    normal = (normalSample.x * input.Tangent) + (normalSample.y * input.Bitangent) + (normalSample.z * input.WorldNormal);

    normal = normalize(normal);

    // Calculate Lambertian diffuse lighting
    float lambertDot = dot(normal, lightDirection);
    if (HalfLambert > 0.5f)
    {
        lambertDot = lambertDot * 0.5f + 0.5f;
        lambertDot = lambertDot * lambertDot; // Square the result for a softer falloff
    }
    float lambert = saturate(lambertDot);

    // Combine ambient and diffuse lighting
    float3 lighting = saturate(AmbientStrength + ((1.0f - AmbientStrength) * lambert * 0.5));


    float4 baseSample = HasBaseTexture > 0.5f
        ? BaseTextureMap.Sample(MaterialSampler, input.TexCoord)
        : float4(1.0f, 1.0f, 1.0f, 1.0f);


    // Apply alpha testing
    if (AlphaTest > 0.5f && baseSample.a < AlphaTestReference)
    {
        discard;
    }

    // Override lighting if self-illumination is enabled
    if (SelfIllum > 0.5f)
    {
        lighting = 1.0f;
    }
    // Emission from a texture and strength, SelfIllum takes priority
    else if (EmissiveBlendStrength > 0 && HasEmissiveTexture > 0.5f)
    {
        float4 emissiveSample = EmissiveTextureMap.Sample(MaterialSampler, input.TexCoord);
        float3 emissiveColor = emissiveSample.rgb * EmissiveBlendStrength;
        lighting += emissiveColor;
    }



    // $color2
    float3 baseColor = lerp(baseSample.rgb * Tint, baseSample.rgb, 1-NoTint);



    float3 litColor = baseColor * lighting;
        
    // Phong lighting
    if (Phong > 0.5f)
    {
        // Use normal map alpha as strength mask, otherwise 1
        float phongMask = HasNormalTexture > 0.5f 
            ? NormalTextureMap.Sample(MaterialSampler, input.TexCoord).a 
            : 1.0f;

        float boost = PhongBoost;

        float exponent = HasPhongExponentTexture > 0.5f
            ? PhongExponentTextureMap.Sample(MaterialSampler, input.TexCoord).r
            : PhongExponent;

        float3 viewDirection = normalize(CameraPosition - input.WorldPosition);
        float3 halfwayDirection = normalize(lightDirection + viewDirection);

        float specAngle = max(dot(halfwayDirection, normal), 0.0f);
        float specular = pow(specAngle, exponent) * boost * phongMask;

        // Integrate Fresnel effect
        float3 fresnelRanges = PhongFresnelRanges;
        float fresnel = Fresnel(normal, viewDirection, fresnelRanges);
        specular *= fresnel;

        // Apply Phong tint and add to the lit color
        litColor += specular * PhongTint;
    }


    float3 hoverColor = float3(0.28f, 0.70f, 1.00f);
    float3 finalColor = lerp(litColor, hoverColor, saturate(HighlightMix));

    return float4(finalColor, baseSample.a);
}

