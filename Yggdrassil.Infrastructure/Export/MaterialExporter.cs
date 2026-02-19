using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Infrastructure.Export
{
    public class MaterialExporter
    {
        public MaterialExporter() { }

        /// <summary>
        /// Generates a VMT (Valve Material Type) string based on the provided material settings.
        /// </summary>
        /// <param name="materialSettings">The settings for the material.</param>
        /// <param name="relativePath">The relative path for the material's textures. Relative to the addon /materials folder</param>
        /// <param name="uniqueTextureNames">A dictionary mapping original texture paths to unique texture names in the case of duplicates.</param>
        /// <returns>A string representing the VMT content.</returns>
        public string GenerateVMT(SourceMaterialSettings materialSettings, string relativePath, Dictionary<string, string> uniqueTextureNames, out List<string>? usedInternalTextures)
        {
            var sb = new StringBuilder();
            var internalsUsed = new List<string>();
            // Start with the shader
            sb.AppendLine($"\"{materialSettings.Shader}\"");
            sb.AppendLine("{");


            // Base Texture / Tintint
            if (!string.IsNullOrEmpty(materialSettings.BaseTexture))
            {
                var baseTexturePath = uniqueTextureNames[materialSettings.BaseTexture];
                sb.AppendLine($"\t\"$basetexture\" \"{relativePath}/{baseTexturePath}\"");
            }
            else
            {
                // Fallback to internal "white"
                sb.AppendLine($"\t\"$basetexture\" \"{relativePath}/white\"");
                internalsUsed.Add("white");
            }

            if (materialSettings.Tint.HasValue)
            {
                var tint = materialSettings.Tint.Value;
                sb.AppendLine($"\t\"$color2\" \"[{tint.R} {tint.G} {tint.B}]\"");
            }

            if (materialSettings.NoTint.HasValue && materialSettings.NoTint.Value)
            {
                sb.AppendLine($"\t\"$notint\" \"1\"");
            }


            // Transparency
            // $alphatest $translucent and $additive are mutually exclusive, but we can still set them all
            // The game will prioritise one of them
            // Blame the user 😎

            if (materialSettings.AlphaTest.HasValue && materialSettings.AlphaTest.Value)
            {
                sb.AppendLine($"\t\"$alphatest\" \"1\"");
                if (materialSettings.AlphaTestReference.HasValue)
                {
                    sb.AppendLine($"\t\"$alphatestreference\" \"{materialSettings.AlphaTestReference.Value}\"");
                }
                if (materialSettings.AllowAlphaToCoverage.HasValue && materialSettings.AllowAlphaToCoverage.Value)
                {
                    sb.AppendLine($"\t\"$allowalphatocoverage\" \"1\"");
                }
            }

            if (materialSettings.NoCull.HasValue && materialSettings.NoCull.Value)
            {
                sb.AppendLine($"\t\"$nocull\" \"1\"");
            }

            if (materialSettings.Translucent.HasValue && materialSettings.Translucent.Value)
            {
                sb.AppendLine($"\t\"$translucent\" \"1\"");
            }
            if (materialSettings.Additive.HasValue && materialSettings.Additive.Value)
            {
                sb.AppendLine($"\t\"$additive\" \"1\"");
            }


            // Lighting
            if (materialSettings.BumpMap != null)
            {
                var bumpMapPath = uniqueTextureNames[materialSettings.BumpMap];
                sb.AppendLine($"\t\"$bumpmap\" \"{relativePath}/{bumpMapPath}\"");
            }

            if (materialSettings.LightWarpTexture != null)
            {
                var lightWarpTexturePath = uniqueTextureNames[materialSettings.LightWarpTexture];
                sb.AppendLine($"\t\"$lightwarptexture\" \"{relativePath}/{lightWarpTexturePath}\"");
            }

            if (materialSettings.HalfLambert.HasValue && materialSettings.HalfLambert.Value)
            {
                sb.AppendLine($"\t\"$halflambert\" \"1\"");
            }

            
            // Self illum and emissive blend do like to break each other
            // Only add one of them
            if (materialSettings.SelfIllum.HasValue && materialSettings.SelfIllum.Value)
            {
                sb.AppendLine($"\t\"$selfillum\" \"1\"");
                if (materialSettings.EmissiveTexture != null)
                {
                    var emissiveTexturePath = uniqueTextureNames[materialSettings.EmissiveTexture];
                    sb.AppendLine($"\t\"$selfillumtexture\" \"{relativePath}/{emissiveTexturePath}\"");
                }
            }
            else if (!string.IsNullOrEmpty(materialSettings.EmissiveTexture))
            {
                var emissiveTexturePath = uniqueTextureNames[materialSettings.EmissiveTexture];
                sb.AppendLine($"\t\"$emissiveblendtexture\" \"{relativePath}/{emissiveTexturePath}\"");
                if (materialSettings.EmissiveBlendStrength.HasValue)
                {
                    sb.AppendLine($"\t\"$emissiveblendstrength\" \"{materialSettings.EmissiveBlendStrength.Value}\"");
                }

                // Also add the extra parameters required for it to work
                // 	$emissiveblendflowtexture "dev/null"
	            //  $emissiveblendscrollvector "[0 0]"
	            // $emissiveblendtexture "models/callybon/null" <-- Should be internal white
                sb.AppendLine($"\t\"$emissiveblendflowtexture\" \"{relativePath}/white\"");
                sb.AppendLine($"\t\"$emissiveblendscrollvector\" \"[0 0]\"");
                sb.AppendLine($"\t\"$emissiveblendtexture\" \"{relativePath}/white\"");
                internalsUsed.Add("white");
            }


            // Reflection and envmap
            if (materialSettings.UseEnvMapProbes.HasValue && materialSettings.UseEnvMapProbes.Value)
            {
                // "$envmap" "env_cubemap"
                sb.AppendLine($"\t\"$envmap\" \"env_cubemap\"");
            }
            else if (!string.IsNullOrEmpty(materialSettings.EnvMap))
            {
                var envMapPath = uniqueTextureNames[materialSettings.EnvMap];
                sb.AppendLine($"\t\"$envmap\" \"{relativePath}/{envMapPath}\"");

                if (!string.IsNullOrEmpty(materialSettings.EnvMapMask))
                {
                    var envMapMaskPath = uniqueTextureNames[materialSettings.EnvMapMask];
                    sb.AppendLine($"\t\"$envmapmask\" \"{relativePath}/{envMapMaskPath}\"");
                }
                if (materialSettings.EnvMapTint.HasValue)
                {
                    var envMapTint = materialSettings.EnvMapTint.Value;
                    sb.AppendLine($"\t\"$envmaptint\" \"[{envMapTint.R} {envMapTint.G} {envMapTint.B}]\"");
                }
                if (materialSettings.EnvMapContrast.HasValue)
                {
                    sb.AppendLine($"\t\"$envmapcontrast\" \"{materialSettings.EnvMapContrast.Value}\"");
                }
            }

            // Phong (Specular)
            if (materialSettings.Phong.HasValue && materialSettings.Phong.Value)
            {
                sb.AppendLine($"\t\"$phong\" \"1\"");

                if (materialSettings.PhongBoost.HasValue)
                {
                    sb.AppendLine($"\t\"$phongboost\" \"{materialSettings.PhongBoost.Value}\"");
                }

                if (materialSettings.PhongFresnelRanges.HasValue)
                {
                    var ranges = materialSettings.PhongFresnelRanges.Value;
                    sb.AppendLine($"\t\"$phongfresnelranges\" \"[{ranges.X} {ranges.Y} {ranges.Z}]\"");
                }

                if (!string.IsNullOrEmpty(materialSettings.PhongExponentTexture))
                {
                    var phongExponentTexturePath = uniqueTextureNames[materialSettings.PhongExponentTexture];
                    sb.AppendLine($"\t\"$phongexponenttexture\" \"{relativePath}/{phongExponentTexturePath}\"");
                }
                else if (materialSettings.PhongExponent.HasValue)
                {
                    sb.AppendLine($"\t\"$phongexponent\" \"{materialSettings.PhongExponent.Value}\"");
                }

                if (materialSettings.PhongTint.HasValue)
                {
                    var phongTint = materialSettings.PhongTint.Value;
                    sb.AppendLine($"\t\"$phongtint\" \"[{phongTint.R} {phongTint.G} {phongTint.B}]\"");
                }

                // Phong mask is handled by baking it into the alpha of the normal map
            }

            // Rimlight
            if (materialSettings.RimLight.HasValue && materialSettings.RimLight.Value)
            {
                sb.AppendLine($"\t\"$rimlight\" \"1\"");
                if (materialSettings.RimLightBoost.HasValue)
                {
                    sb.AppendLine($"\t\"$rimlightboost\" \"{materialSettings.RimLightBoost.Value}\"");
                }
                if (materialSettings.RimLightExponent.HasValue)
                {
                    sb.AppendLine($"\t\"$rimlightexponent\" \"{materialSettings.RimLightExponent.Value}\"");
                }
                if (materialSettings.RimLightTint.HasValue)
                {
                    var rimLightTint = materialSettings.RimLightTint.Value;
                    sb.AppendLine($"\t\"$rimlighttint\" \"[{rimLightTint.R} {rimLightTint.G} {rimLightTint.B}]\"");
                }
            }



            sb.AppendLine("}");

            usedInternalTextures = internalsUsed.Count > 0 ? internalsUsed : null;

            return sb.ToString();
        }
    }
}
