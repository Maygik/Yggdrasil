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
        public List<string> CdMaterialsPaths { get; set; } = new List<string> { "" };   // Relative paths to read materials from. "" means root materials folder.
        public List<Bodygroup> Bodygroups { get; set; } = new();      // Bodygroup definitions. Tuple: (bodygroup name, list of submeshes)

        public string SurfaceProp { get; set; } = "flesh";                              // $surfaceprop value.
        public string IllumBone { get; set; } = "ValveBiped.Bip01_Pelvis";              // $illumposition bone name. Used to reduce lighting artifacts.

        public AnimationProfile AnimationProfile { get; set; } = AnimationProfile.None;
        public QcFeatures Features { get; set; } = new();
    }

    public record Bodygroup (string Name, List<string> Submeshes);
}
