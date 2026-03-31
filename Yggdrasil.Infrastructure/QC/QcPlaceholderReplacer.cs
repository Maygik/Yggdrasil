using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Domain.QC;

namespace Yggdrasil.Infrastructure.QC
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

            // $cdmaterials
            // Generate conditional block for multiple cdmaterials paths
            var cdmsb = new StringBuilder();
            foreach (var mat in config.CdMaterialsPaths)
            {
                cdmsb.AppendLine($"$cdmaterials \"{mat}\"");
            }
            cdmsb.AppendLine("$cdmaterials \"\"");
            result = result.Replace("@MATERIAL_LINES@", cdmsb.ToString());

            result = result.Replace("@SURFACE_PROP@", config.SurfaceProp);
            return result;
        }
    }
}
