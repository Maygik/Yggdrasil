using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Color3 = Yggdrasil.Types.Color3;
using Vector3 = Yggdrasil.Types.Vector3;

namespace Yggdrasil.Domain.Scene
{
    public class SourceMaterialSettings
    {
        public string Name { get; set; } = "Material"; // The name of the material (VMT filename without extension). Used for reference and export.

        public string Shader { get; set; } = "VertexLitGeneric"; // The shader used by this material. Only VertexLitGeneric is supported for now, but this can be extended in the future if needed.

        public bool Adjusted { get; set; } = false; // Whether this material has been adjusted from the default settings. Used to determine if we need to deal with it at all.

        // ----- Parameters -----
        // All null by default, only assigned a value if they are used.
        // This way we can easily determine which parameters are actually used and only export those to the VMT, keeping it clean and minimal.

        // Basics
        public string? BaseTexture { get; set; } = null; // Path to the original image file used for $basetexture. Full path on disk.
                                                         // Possible TODO: Maybe add support for $detail
                                                         // But it's annoying so maybe not

        // Adjustment
        public Color3? Tint { get; set; } = null; // $color2 parameter. Used to tint the material. Default is white (no tint).
        public bool? NoTint { get; set; } = null; // $notint parameter. Whether to disable tinting. Default is false (allow tinting).

        // Transparency
        public bool? AlphaTest { get; set; } = null; // Whether to use alpha testing for transparency. Default is false (opaque).
        public float? AlphaTestReference { get; set; } = null; // $alphatestreference parameter. Reference value for alpha testing. Default is 0.5 (Hide if alpha < 0.5).
        public bool? AllowAlphaToCoverage { get; set; } = null; // $allowalphatocoverage parameter. Whether to allow alpha to coverage for anti-aliasing of alpha tested materials. Default is false (no alpha to coverage).

        public bool? NoCull { get; set; } = null; // $nocull parameter. Whether to disable backface culling. Default is false (enable culling).
        public bool? Translucent { get; set; } = null; // Whether the material is translucent. Default is false (opaque).
        public bool? Additive { get; set; } = null; // Whether the material uses additive blending. Default is false (normal blending).

        // Lighting
        public string? BumpMap { get; set; } = null; // Path to the original image file used for $bumpmap. Full path on disk. Really a normal map, but Source calls it bumpmap for some reason.
        public string? LightWarpTexture { get; set; } = null; // Path to the original image file used for $lightwarptexture. Full path on disk. Used to remap lighting, e.g. for toon shading.
        public bool? HalfLambert { get; set; } = null; // $halflambert parameter. Whether to use half-lambert lighting, which makes the lighting softer and less harsh. Default is false (use normal lambert lighting).
        public bool? SelfIllum { get; set; } = null; // $selfillum parameter. Whether the material is self-illuminated (unaffected by lighting). Default is false (affected by lighting).

        // Emissive blend
        // Not supporting the movement effects, but using as a better version of selfillum
        public string? EmissiveTexture { get; set; } = null; // Path to the original image file used for emissiveblend stuff. Full path on disk. Used for emissive lighting, e.g. for glowing parts.
        public float? EmissiveBlendStrength { get; set; } = null; // $emissiveblendstrength parameter. Strength of the emissive effect. Default is 1.0 (full strength).

        // Reflection
        public bool? UseEnvMapProbes { get; set; } = null; // If true, use env_cubemap for the envmap instead of the one specified in EnvMap. Reflects the environment rather than using a set one.
        public string? EnvMap { get; set; } = null; // Path to the original image file used for $envmap. Full path on disk. Used for reflections.
        public string? EnvMapMask { get; set; } = null; // Path to the original image file used for $envmapmask. Full path on disk. Used to mask reflections, e.g. for partial reflections or fresnel effects. Not compatible with $bumpmap.
        public Color3? EnvMapTint { get; set; } = null; // $envmaptint parameter. Used to tint the reflections. Default is white (no tint).
        public float? EnvMapContrast { get; set; } = null; // $envmapcontrast parameter. Used to adjust the contrast of the reflections. Default is 0.0.


        public bool? Phong { get; set; } = null; // $phong parameter. Whether to use phong shading for reflections. Default is false (use normal reflection).
        public float? PhongBoost { get; set; } = null; // $phongboost parameter. Strength of the phong effect. Default is 1.0.
        public int? PhongExponent { get; set; } = null; // $phongexponent parameter. Exponent for the phong effect. Higher values make the reflections sharper. Default is 5.
        public string? PhongExponentTexture { get; set; } = null; // Path to the original image file used for $phongexponenttexture. Full path on disk. Used to vary the phong exponent across the surface, e.g. for sharper reflections on edges or specific areas.
        public Vector3? PhongFresnelRanges { get; set; } = null; // $phongfresnelranges parameter. Fresnel ranges for the phong effect. Default is (0.0, 0.5, 1.0).
        public Color3? PhongTint { get; set; } = null; // $phongtint parameter. Used to tint the phong reflections. Default is white (no tint). Also affects the rim light if $rimlight is enabled.


        public bool? RimLight { get; set; } = null; // $rimlight parameter. Whether to use rim lighting for reflections. Default is false (no rim lighting).
        public int? RimLightExponent { get; set; } = null; // $rimlightexponent parameter. Exponent for the rim light effect. Higher values make the rim light sharper. Default is 2.
        public float? RimLightBoost { get; set; } = null; // $rimlightboost parameter. Strength of the rim light effect. Default is 0.2.
        public Color3? RimLightTint { get { return PhongTint; } set { PhongTint = value; } } // Rim light uses the same tint as the phong reflections, so we can just reuse that parameter.

        public SourceMaterialSettings DeepClone()
        {
            return new SourceMaterialSettings
            {
                Name = Name,
                Shader = Shader,
                Adjusted = Adjusted,
                BaseTexture = BaseTexture,
                Tint = Tint,
                NoTint = NoTint,
                AlphaTest = AlphaTest,
                AlphaTestReference = AlphaTestReference,
                AllowAlphaToCoverage = AllowAlphaToCoverage,
                NoCull = NoCull,
                Translucent = Translucent,
                Additive = Additive,
                BumpMap = BumpMap,
                LightWarpTexture = LightWarpTexture,
                HalfLambert = HalfLambert,
                SelfIllum = SelfIllum,
                EmissiveTexture = EmissiveTexture,
                EmissiveBlendStrength = EmissiveBlendStrength,
                UseEnvMapProbes = UseEnvMapProbes,
                EnvMap = EnvMap,
                EnvMapMask = EnvMapMask,
                EnvMapTint = EnvMapTint,
                EnvMapContrast = EnvMapContrast,
                Phong = Phong,
                PhongBoost = PhongBoost,
                PhongExponent = PhongExponent,
                PhongExponentTexture = PhongExponentTexture,
                PhongFresnelRanges = PhongFresnelRanges,
                PhongTint = PhongTint,
                RimLight = RimLight,
                RimLightExponent = RimLightExponent,
                RimLightBoost = RimLightBoost
            };
        }
    }
}
