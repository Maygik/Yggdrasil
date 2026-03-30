using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix4x4 = Yggdrasil.Types.Matrix4x4;
using Vector3 = Yggdrasil.Types.Vector3;

namespace Yggdrasil.Domain.Scene
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
                    new KeyValuePair<string, SourceMaterialSettings>(kvp.Key, kvp.Value.DeepClone())))
            };
            return clone;
        }

        public void RenameBone(string currentName, string newName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentName);
            ArgumentException.ThrowIfNullOrWhiteSpace(newName);

            RenameBones(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [currentName] = newName
            });
        }

        public void RenameBones(IReadOnlyDictionary<string, string> boneRenames)
        {
            ArgumentNullException.ThrowIfNull(boneRenames);

            if (boneRenames.Count == 0)
            {
                return;
            }

            var normalizedRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rename in boneRenames)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(rename.Key);
                ArgumentException.ThrowIfNullOrWhiteSpace(rename.Value);

                if (normalizedRenames.TryGetValue(rename.Key, out var existingTarget)
                    && !string.Equals(existingTarget, rename.Value, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Bone '{rename.Key}' cannot be renamed to both '{existingTarget}' and '{rename.Value}'.");
                }

                if (string.Equals(rename.Key, rename.Value, StringComparison.Ordinal))
                {
                    continue;
                }

                normalizedRenames[rename.Key] = rename.Value;
            }

            if (normalizedRenames.Count == 0)
            {
                return;
            }

            RenameBoneNodes(normalizedRenames);
            RenameMeshBoneWeights(normalizedRenames);
        }

        private void RenameBoneNodes(IReadOnlyDictionary<string, string> boneRenames)
        {
            if (RootBone == null)
            {
                return;
            }

            var allBones = RootBone.GetAllDescendantsAndSelf();
            var existingBoneNames = new HashSet<string>(allBones.Select(b => b.Name), StringComparer.OrdinalIgnoreCase);
            var sourceNamesInScene = boneRenames.Keys
                .Where(existingBoneNames.Contains)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (sourceNamesInScene.Count == 0)
            {
                return;
            }

            var nodeRenames = boneRenames
                .Where(rename => sourceNamesInScene.Contains(rename.Key))
                .ToList();

            var duplicateTargets = nodeRenames
                .GroupBy(rename => rename.Value, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Select(rename => rename.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateTargets.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Multiple scene bones cannot be renamed to the same target: {string.Join(", ", duplicateTargets)}.");
            }

            var unaffectedBoneNames = new HashSet<string>(existingBoneNames, StringComparer.OrdinalIgnoreCase);
            unaffectedBoneNames.ExceptWith(sourceNamesInScene);

            foreach (var rename in nodeRenames)
            {
                if (unaffectedBoneNames.Contains(rename.Value))
                {
                    throw new InvalidOperationException(
                        $"Cannot rename bone '{rename.Key}' to '{rename.Value}' because that bone already exists in the scene.");
                }
            }

            var renamedBones = new List<(Bone Bone, string FinalName)>();
            int renameIndex = 0;

            foreach (var bone in allBones)
            {
                if (!boneRenames.TryGetValue(bone.Name, out var finalName))
                {
                    continue;
                }

                renamedBones.Add((bone, finalName));
                bone.Name = $"__ygg_tmp_bone_rename_{renameIndex++}_{Guid.NewGuid():N}";
            }

            foreach (var renamedBone in renamedBones)
            {
                renamedBone.Bone.Name = renamedBone.FinalName;
            }
        }

        private void RenameMeshBoneWeights(IReadOnlyDictionary<string, string> boneRenames)
        {
            foreach (var meshGroup in MeshGroups)
            {
                foreach (var mesh in meshGroup.Meshes)
                {
                    for (int i = 0; i < mesh.BoneWeights.Count; i++)
                    {
                        var originalWeights = mesh.BoneWeights[i];
                        var renamedWeights = new List<Tuple<string, float>>(originalWeights.Count);
                        var changed = false;

                        foreach (var weight in originalWeights)
                        {
                            if (boneRenames.TryGetValue(weight.Item1, out var renamedBone))
                            {
                                renamedWeights.Add(Tuple.Create(renamedBone, weight.Item2));
                                changed = true;
                            }
                            else
                            {
                                renamedWeights.Add(weight);
                            }
                        }

                        if (changed)
                        {
                            mesh.BoneWeights[i] = renamedWeights;
                        }
                    }
                }
            }
        }
    }
}
