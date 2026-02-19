using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    public class SceneModel
    {
        public string Name { get; set; } = string.Empty;

        // List of mesh groups in this model. A mesh group is the collection of meshes at a node in assimp.
        public List<MeshGroup> MeshGroups { get; set; } = new List<MeshGroup>();
        
        // The root bone of the skeleton hierarchy. This will be null if the model has no skeleton ie a prop.
        public Bone? RootBone { get; set; } = null;

        // Material settings for this model, keyed by material name. This is used during QC generation to determine $cdmaterials paths and other material-related settings.
        public Dictionary<string, SourceMaterialSettings> MaterialSettings { get; set; } = new Dictionary<string, SourceMaterialSettings>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Model: {Name}");

            sb.AppendLine($"Root Bone: {(RootBone != null ? RootBone.Name : "None")}");
            void PrintBone(Bone bone, string indent = "")
            {
                sb.AppendLine($"{indent}{bone.Name}");
                foreach (var child in bone.Children)
                {
                    if (child is Bone childBone)
                        PrintBone(childBone, indent + "\t");
                }
            }

            // Recursively print the bone hierarchy
            if (RootBone != null)
            {
                PrintBone(RootBone);
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();


            // Materials
            sb.AppendLine($"Materials: {MaterialSettings.Count}");
            foreach (var kvp in MaterialSettings)
            {
                sb.AppendLine($"\tMaterial: {kvp.Key}");
            }


            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"Mesh Groups: {MeshGroups.Count}");
            foreach (var mg in MeshGroups)
            {
                sb.AppendLine($"\t{mg}");
            }

            return sb.ToString();
        }
    }
}
