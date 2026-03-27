using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Buffers;

[StructLayout(LayoutKind.Sequential)]
internal struct PerMaterialConstants
{
    public float TintR;
    public float TintG;
    public float TintB;
    public float HasBaseTexture;
    public float HasNormalTexture;
    public float HasEmissiveTexture;
    public float HasLightWarpTexture;
    public float HasEnvMapTexture;
    public float HasEnvMapMaskTexture;
    public float HasPhongExponentTexture;
    public float HasPhongMaskTexture;
    public float NoTint;
    public float AlphaTest;
    public float AlphaTestReference;
    public float AllowAlphaToCoverage;
    public float NoCull;
    public float Translucent;
    public float Additive;
    public float HalfLambert;
    public float SelfIllum;
    public float EmissiveBlendStrength;
    public float UseEnvMapProbes;
    public float EnvMapContrast;
    public float Phong;
    public float EnvMapTintR;
    public float EnvMapTintG;
    public float EnvMapTintB;
    public float RimLight;
    public float PhongBoost;
    public float PhongExponent;
    public float RimLightExponent;
    public float RimLightBoost;
    public float PhongFresnelX;
    public float PhongFresnelY;
    public float PhongFresnelZ;
    public float Adjusted;
    public float PhongTintR;
    public float PhongTintG;
    public float PhongTintB;
    public float Padding0;
    public float RimLightTintR;
    public float RimLightTintG;
    public float RimLightTintB;
    public float Padding1;
}
