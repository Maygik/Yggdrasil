using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.QC;

namespace Yggdrassil.Infrastructure.QC
{
    public static class QcPlaceholderReplacer
    {
        public static string ReplacePlaceholders(string template, QcConfig config)
        {
            var result = template.Replace("@MODEL_PATH@", config.ModelPath);

            if (config.Bodygroups != null && config.Bodygroups.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var bodygroup in config.Bodygroups)
                {
                    sb.AppendLine($"$bodygroup \"{bodygroup.Name}\"");
                    sb.AppendLine("{");
                    foreach (var option in bodygroup.Submeshes)
                    {
                        if (string.IsNullOrEmpty(option))
                            sb.AppendLine($"\tblank");
                        else
                            sb.AppendLine($"\tstudio \"{option}\"");
                    }
                    sb.AppendLine("}");
                }
                result = result.Replace("@BODYGROUPS@", sb.ToString());
            }
            else
            {
                result = result.Replace("@BODYGROUPS@", string.Empty);
            }

            // Handle $cdmaterials. If multiple paths, we need to generate a conditional block.
            if (config.CdMaterialsPaths.Count == 1)
            {
                result = result.Replace("@MATERIAL_LINES@", config.CdMaterialsPaths[0]);
            }
            else
            {
                // Generate conditional block for multiple cdmaterials paths
                var sb = new StringBuilder();
                foreach (var mat in config.CdMaterialsPaths)
                {
                    sb.AppendLine($"$cdmaterials \"{mat}\"");
                }
                sb.AppendLine("$cdmaterials \"\"");
                result = result.Replace("@MATERIAL_LINES@", sb.ToString());
            }
            result = result.Replace("@SURFACE_PROP@", config.SurfaceProp);
            result = result.Replace("@ILLUM_POSITION@", config.IllumBone);
            return result;
        }
    }
}
