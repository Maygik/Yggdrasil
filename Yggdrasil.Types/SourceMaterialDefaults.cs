namespace Yggdrasil.Types;

public static class SourceMaterialDefaults
{
    public const string Shader = "VertexLitGeneric";
    public const string TexturePath = "";

    public static readonly Color3 Color2 = Color3.White;
    public static readonly Color3 EnvMapTint = Color3.White;
    public static readonly Color3 PhongTint = Color3.White;
    public static readonly Color3 RimLightTint = Color3.White;

    public const bool Toggle = false;

    public const float AlphaTestReference = 0.5f;
    public const float EmissiveBlendStrength = 1.0f;
    public const float EnvMapContrast = 0.0f;
    public const float PhongBoost = 1.0f;
    public const int PhongExponent = 5;
    public static readonly Vector3 PhongFresnelRanges = new(0.0f, 0.5f, 1.0f);
    public const int RimLightExponent = 2;
    public const float RimLightBoost = 0.2f;
}
