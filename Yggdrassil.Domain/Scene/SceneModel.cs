using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3 = Yggdrassil.Types.Vector3;

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

        // Remove all scale from the model by applying it to vertices and bones
        public void ApplyScale()
        {
            if (RootBone != null)
            {
                void ApplyScaleToBone(Bone bone, Vector3 parentScale)
                {
                    var currentScale = new Vector3
                    {
                        X = parentScale.X * bone.LocalScale.X,
                        Y = parentScale.Y * bone.LocalScale.Y,
                        Z = parentScale.Z * bone.LocalScale.Z
                    };

                    var localPos = bone.LocalPosition;
                    localPos.X *= parentScale.X;
                    localPos.Y *= parentScale.Y;
                    localPos.Z *= parentScale.Z;
                    bone.LocalPosition = localPos;


                    bone.LocalScale = Vector3.One; // Reset local scale to 1 after applying it

                    foreach (var child in bone.Children)
                    {
                        if (child is Bone childBone)
                            ApplyScaleToBone(childBone, currentScale);
                    }
                }
                ApplyScaleToBone(RootBone, Vector3.One);
            }

            foreach (var mg in MeshGroups)
            {
                foreach (var mesh in mg.Meshes)
                {
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                    {
                        var v = mesh.Vertices[i];
                        var localScale = mg.LocalScale;
                        v.X *= localScale.X;
                        v.Y *= localScale.Y;
                        v.Z *= localScale.Z;
                        mesh.Vertices[i] = v;

                        // Normals and UVs etc don't need to be scaled, only the vertex positions
                    }
                    mg.LocalScale = Vector3.One; // Reset local scale to 1 after applying it
                }
            }


        }

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
                sb.AppendLine($"\tMaterial: {kvp.Key} {(kvp.Value.Adjusted ? "(Adjusted)" : "(Unedited)")}");
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

        /// <summary>
        /// Creates a deep copy of the entire scene model including meshes, bones, and material settings
        /// </summary>
        public SceneModel DeepClone()
        {
            var clone = new SceneModel
            {
                Name = Name,
                MeshGroups = MeshGroups.Select(mg => mg.DeepClone()).ToList(),
                RootBone = RootBone?.DeepClone(),
                MaterialSettings = new Dictionary<string, SourceMaterialSettings>(MaterialSettings.Select(kvp => 
                    new KeyValuePair<string, SourceMaterialSettings>(kvp.Key, new SourceMaterialSettings 
                    { 
                        Name = kvp.Value.Name,
                        Adjusted = kvp.Value.Adjusted
                    })))
            };
            return clone;
        }
    }
}
