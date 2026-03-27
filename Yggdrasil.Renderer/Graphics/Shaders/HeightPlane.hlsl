struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(VSOutput input) : SV_TARGET
{
    return float4(1.0f, 0.54f, 0.12f, 0.28f);
}
