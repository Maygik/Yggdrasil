using System;
using System.Collections.Generic;
using System.Linq;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Presentation.Rendering;

public sealed class SceneToRenderSnapshotMapper
{
    public RenderSceneSnapshot MapScene(SceneModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var skeleton = BuildSkeletonSnapshot(scene.RootBone);
        var materials = BuildMaterialMap(scene);
        var meshes = BuildMeshSnapshots(scene, skeleton?.BoneIndicesByName);

        EnsureReferencedMaterialsExist(meshes, materials);

        return new RenderSceneSnapshot
        {
            Name = scene.Name,
            Meshes = meshes,
            Materials = materials,
            Skeleton = skeleton,
            Bounds = CalculateBounds(meshes)
        };
    }

    public RenderMaterialSnapshot MapMaterial(string materialName, SourceMaterialSettings material)
    {
        ArgumentNullException.ThrowIfNull(material);

        return BuildMaterialSnapshot(materialName, material);
    }

    private static RenderMaterialSnapshot BuildMaterialSnapshot(string materialName, SourceMaterialSettings material)
    {
        var resolvedName = string.IsNullOrWhiteSpace(materialName)
            ? material.Name
            : materialName;

        return new RenderMaterialSnapshot
        {
            Name = string.IsNullOrWhiteSpace(resolvedName) ? "Material" : resolvedName,
            Shader = string.IsNullOrWhiteSpace(material.Shader) ? "VertexLitGeneric" : material.Shader,
            Adjusted = material.Adjusted,
            BaseTexture = material.BaseTexture,
            Tint = material.Tint,
            NoTint = material.NoTint,
            AlphaTest = material.AlphaTest,
            AlphaTestReference = material.AlphaTestReference,
            AllowAlphaToCoverage = material.AllowAlphaToCoverage,
            NoCull = material.NoCull,
            Translucent = material.Translucent,
            Additive = material.Additive,
            BumpMap = material.BumpMap,
            LightWarpTexture = material.LightWarpTexture,
            HalfLambert = material.HalfLambert,
            SelfIllum = material.SelfIllum,
            EmissiveTexture = material.EmissiveTexture,
            EmissiveBlendStrength = material.EmissiveBlendStrength,
            UseEnvMapProbes = material.UseEnvMapProbes,
            EnvMap = material.EnvMap,
            EnvMapMask = material.EnvMapMask,
            EnvMapTint = material.EnvMapTint,
            EnvMapContrast = material.EnvMapContrast,
            Phong = material.Phong,
            PhongBoost = material.PhongBoost,
            PhongExponent = material.PhongExponent,
            PhongExponentTexture = material.PhongExponentTexture,
            PhongFresnelRanges = material.PhongFresnelRanges,
            PhongTint = material.PhongTint,
            RimLight = material.RimLight,
            RimLightExponent = material.RimLightExponent,
            RimLightBoost = material.RimLightBoost,
            RimLightTint = material.RimLightTint
        };
    }

    private static Dictionary<string, RenderMaterialSnapshot> BuildMaterialMap(SceneModel scene)
    {
        var materials = new Dictionary<string, RenderMaterialSnapshot>(StringComparer.Ordinal);

        foreach (var entry in scene.MaterialSettings)
        {
            if (entry.Value == null)
            {
                continue;
            }

            materials[entry.Key] = BuildMaterialSnapshot(entry.Key, entry.Value);
        }

        return materials;
    }

    private static List<RenderMeshSnapshot> BuildMeshSnapshots(
        SceneModel scene,
        IReadOnlyDictionary<string, int>? boneIndicesByName)
    {
        var meshes = new List<RenderMeshSnapshot>();

        foreach (var meshGroup in scene.MeshGroups)
        {
            var modelMatrix = meshGroup.WorldMatrix.Copy();

            foreach (var mesh in meshGroup.Meshes)
            {
                meshes.Add(new RenderMeshSnapshot
                {
                    Name = BuildMeshName(meshGroup, mesh),
                    MaterialName = mesh.Material ?? string.Empty,
                    ModelMatrix = modelMatrix.Copy(),
                    Vertices = mesh.Vertices.ToArray(),
                    Normals = mesh.Normals.ToArray(),
                    Tangents = mesh.Tangents.ToArray(),
                    BiTangents = mesh.BiTangents.ToArray(),
                    UVs = mesh.UVs.ToArray(),
                    Indices = BuildIndices(mesh.Faces),
                    SkinWeights = BuildSkinWeights(mesh, boneIndicesByName)
                });
            }
        }

        return meshes;
    }

    private static RenderSkeletonSnapshot? BuildSkeletonSnapshot(Bone? rootBone)
    {
        if (rootBone == null)
        {
            return null;
        }

        var bones = new List<RenderBoneSnapshot>();
        var boneIndicesByName = new Dictionary<string, int>(StringComparer.Ordinal);

        void AddBone(Bone bone, int parentIndex)
        {
            var boneIndex = bones.Count;
            if (!boneIndicesByName.ContainsKey(bone.Name))
            {
                boneIndicesByName.Add(bone.Name, boneIndex);
            }

            var worldMatrix = bone.WorldMatrix.Copy();
            bones.Add(new RenderBoneSnapshot
            {
                Name = bone.Name,
                ParentIndex = parentIndex,
                IsDeform = bone.IsDeform,
                LocalMatrix = bone.LocalMatrix.Copy(),
                WorldMatrix = worldMatrix,
                InverseBindMatrix = TryInvert(worldMatrix)
            });

            foreach (var child in bone.Children.OfType<Bone>())
            {
                AddBone(child, boneIndex);
            }
        }

        AddBone(rootBone, -1);

        return new RenderSkeletonSnapshot
        {
            Bones = bones,
            BoneIndicesByName = boneIndicesByName
        };
    }

    private static IReadOnlyList<uint> BuildIndices(IReadOnlyList<Face> faces)
    {
        var indices = new uint[faces.Count * Face.VertexCount];
        var index = 0;

        foreach (var face in faces)
        {
            indices[index++] = (uint)face.Vertex1;
            indices[index++] = (uint)face.Vertex3;
            indices[index++] = (uint)face.Vertex2;
        }

        return indices;
    }

    private static IReadOnlyList<RenderVertexWeights> BuildSkinWeights(
        MeshData mesh,
        IReadOnlyDictionary<string, int>? boneIndicesByName)
    {
        if (mesh.Vertices.Count == 0)
        {
            return Array.Empty<RenderVertexWeights>();
        }

        var weights = new RenderVertexWeights[mesh.Vertices.Count];

        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var vertexWeights = vertexIndex < mesh.BoneWeights.Count
                ? mesh.BoneWeights[vertexIndex]
                : null;

            weights[vertexIndex] = PackVertexWeights(vertexWeights, boneIndicesByName);
        }

        return weights;
    }

    private static RenderVertexWeights PackVertexWeights(
        IReadOnlyList<Tuple<string, float>>? vertexWeights,
        IReadOnlyDictionary<string, int>? boneIndicesByName)
    {
        if (vertexWeights == null || vertexWeights.Count == 0 || boneIndicesByName == null || boneIndicesByName.Count == 0)
        {
            return EmptyWeights;
        }

        var weightedBones = vertexWeights
            .Where(weight => weight != null && !string.IsNullOrWhiteSpace(weight.Item1) && weight.Item2 > 0.0f)
            .Select(weight => new WeightedBone(
                boneIndicesByName.TryGetValue(weight.Item1, out var boneIndex) ? boneIndex : -1,
                weight.Item2))
            .Where(weight => weight.BoneIndex >= 0)
            .OrderByDescending(weight => weight.Weight)
            .Take(4)
            .ToArray();

        if (weightedBones.Length == 0)
        {
            return EmptyWeights;
        }

        var totalWeight = weightedBones.Sum(weight => weight.Weight);
        if (totalWeight <= float.Epsilon)
        {
            return EmptyWeights;
        }

        return new RenderVertexWeights(
            weightedBones.Length > 0 ? weightedBones[0].BoneIndex : -1,
            weightedBones.Length > 1 ? weightedBones[1].BoneIndex : -1,
            weightedBones.Length > 2 ? weightedBones[2].BoneIndex : -1,
            weightedBones.Length > 3 ? weightedBones[3].BoneIndex : -1,
            weightedBones.Length > 0 ? weightedBones[0].Weight / totalWeight : 0.0f,
            weightedBones.Length > 1 ? weightedBones[1].Weight / totalWeight : 0.0f,
            weightedBones.Length > 2 ? weightedBones[2].Weight / totalWeight : 0.0f,
            weightedBones.Length > 3 ? weightedBones[3].Weight / totalWeight : 0.0f);
    }

    private static void EnsureReferencedMaterialsExist(
        IReadOnlyList<RenderMeshSnapshot> meshes,
        IDictionary<string, RenderMaterialSnapshot> materials)
    {
        foreach (var mesh in meshes)
        {
            if (string.IsNullOrWhiteSpace(mesh.MaterialName) || materials.ContainsKey(mesh.MaterialName))
            {
                continue;
            }

            materials[mesh.MaterialName] = new RenderMaterialSnapshot
            {
                Name = mesh.MaterialName
            };
        }
    }

    private static SceneBounds CalculateBounds(IReadOnlyList<RenderMeshSnapshot> meshes)
    {
        var hasBounds = false;
        var min = Vector3.Zero;
        var max = Vector3.Zero;

        foreach (var mesh in meshes)
        {
            foreach (var vertex in mesh.Vertices)
            {
                var transformed = TransformPoint(mesh.ModelMatrix, vertex);

                if (!hasBounds)
                {
                    min = transformed;
                    max = transformed;
                    hasBounds = true;
                    continue;
                }

                min = new Vector3(
                    MathF.Min(min.X, transformed.X),
                    MathF.Min(min.Y, transformed.Y),
                    MathF.Min(min.Z, transformed.Z));

                max = new Vector3(
                    MathF.Max(max.X, transformed.X),
                    MathF.Max(max.Y, transformed.Y),
                    MathF.Max(max.Z, transformed.Z));
            }
        }

        return new SceneBounds
        {
            Min = min,
            Max = max
        };
    }

    private static Vector3 TransformPoint(Matrix4x4 matrix, Vector3 point)
    {
        var transformed = matrix * new Vector4(point.X, point.Y, point.Z, 1.0f);
        if (MathF.Abs(transformed.W) > float.Epsilon && transformed.W != 1.0f)
        {
            return transformed.XYZ / transformed.W;
        }

        return transformed.XYZ;
    }

    private static Matrix4x4 TryInvert(Matrix4x4 matrix)
    {
        return matrix.TryInvertAffine(out var inverse)
            ? inverse
            : Matrix4x4.Identity;
    }

    private static string BuildMeshName(MeshGroup meshGroup, MeshData mesh)
    {
        if (!string.IsNullOrWhiteSpace(mesh.Name))
        {
            return mesh.Name;
        }

        if (!string.IsNullOrWhiteSpace(meshGroup.Name))
        {
            return $"{meshGroup.Name}/Mesh";
        }

        return "Mesh";
    }

    private readonly record struct WeightedBone(int BoneIndex, float Weight);

    private static readonly RenderVertexWeights EmptyWeights = new(
        -1,
        -1,
        -1,
        -1,
        0.0f,
        0.0f,
        0.0f,
        0.0f);
}
