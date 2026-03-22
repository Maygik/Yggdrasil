using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.QC
{
    /// <summary>
    /// Describes all qc-relevant configuration for a model.
    /// $modelname, $cdmaterials, $surfaceprop etc.
    /// This is used during QC generation. No logic, pure config.
    /// </summary>
    public sealed class QcConfig
    {
        public string ModelPath { get; set; } = "myaddon/my_model";                     // $modelname path, relative to models/ folder, without file extension.        
        public List<string> CdMaterialsPaths { get; set; } = new List<string> { };   // Relative paths to read materials from. "" means root materials folder.
        public List<Bodygroup> Bodygroups { get; set; } = new();      // Bodygroup definitions. Tuple: (bodygroup name, list of submeshes)

        public string SurfaceProp { get; set; } = "flesh";                              // $surfaceprop value.
        public string IllumBone { get; set; } = "ValveBiped.Bip01_Pelvis";              // $illumposition bone name. Used to reduce lighting artifacts.

        
        public AnimationProfile AnimationProfile { get; set; } = AnimationProfile.None;
        public QcFeatures Features { get; set; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"$modelname \"{ModelPath}\"");
            sb.AppendLine($"$cdmaterials \"{string.Join(" ", CdMaterialsPaths)}\"");
            sb.AppendLine($"$surfaceprop \"{SurfaceProp}\"");
            sb.AppendLine($"$illumposition \"{IllumBone}\"");
            if (Bodygroups.Count > 0)
            {
                sb.AppendLine("$bodygroups");
                foreach (var bg in Bodygroups)
                {
                    sb.AppendLine($"\t\"{bg.Name}\" {{ {string.Join(" ", bg.Submeshes.Select(s => s ?? "blank"))} }}");
                }
            }
            return sb.ToString();
        }
    }

    public class Bodygroup
    {
        public string Name { get; set; }
        public List<string?> Submeshes { get; set; }

        public Bodygroup(string name, List<string?> submeshes)
        {
            Name = name;
            Submeshes = submeshes;
        }
    }
}
