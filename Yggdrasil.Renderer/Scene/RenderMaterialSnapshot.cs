using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class RenderMaterialSnapshot
{

    // Snapshot settings
    public string Name { get; init; } = string.Empty;
    public string Shader { get; init; } = "VertexLitGeneric";
    public bool Adjusted { get; init; }


    // Colouring
    public string? BaseTexture { get; init; }
    public Color3? Tint { get; init; }
    public bool? NoTint { get; init; }


    // Transparency
    public bool? AlphaTest { get; init; }
    public float? AlphaTestReference { get; init; }
    public bool? AllowAlphaToCoverage { get; init; }

    public bool? NoCull { get; init; }
    public bool? Translucent { get; init; }
    public bool? Additive { get; init; }


    // Lighting
    public string? BumpMap { get; init; }
    public string? LightWarpTexture { get; init; }
    public bool? HalfLambert { get; init; }


    // Emission
    public bool? SelfIllum { get; init; }
    public string? EmissiveTexture { get; init; }
    public float? EmissiveBlendStrength { get; init; }
    public bool? UseEnvMapProbes { get; init; }


    // Reflection
    public string? EnvMap { get; init; }
    public string? EnvMapMask { get; init; }
    public Color3? EnvMapTint { get; init; }
    public float? EnvMapContrast { get; init; }


    // Specular
    public bool? Phong { get; init; }
    public float? PhongBoost { get; init; }
    public int? PhongExponent { get; init; }
    public string? PhongExponentTexture { get; init; }
    public Vector3? PhongFresnelRanges { get; init; }
    public Color3? PhongTint { get; init; }


    // Rim lighting
    public bool? RimLight { get; init; }
    public int? RimLightExponent { get; init; }
    public float? RimLightBoost { get; init; }
    public Color3? RimLightTint { get; init; }
}
