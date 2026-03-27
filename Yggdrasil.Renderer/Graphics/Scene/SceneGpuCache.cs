using System;
using System.Collections.Generic;
using Yggdrasil.Renderer.Graphics.Buffers;
using Yggdrasil.Renderer.Graphics.Shaders;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;
using Vortice.Direct3D11;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class SceneGpuCache : IDisposable
{
    private readonly Dictionary<string, MaterialGpuResources> _materials = new(StringComparer.Ordinal);
    private readonly List<MeshGpuResources> _meshes = [];

    public IReadOnlyList<RenderDrawItem> DrawItems { get; private set; } = [];

    // Loads the provided scene snapshot into GPU resources, replacing any existing cached data. If the provided snapshot is null, the cache will be cleared.
    public void LoadScene(DeviceResources deviceResources, RenderSceneSnapshot? scene)
    {
        ArgumentNullException.ThrowIfNull(deviceResources);

        Clear();

        if (scene == null)
        {
            return;
        }

        foreach (var materialEntry in scene.Materials)
        {
            _materials[materialEntry.Key] = CreateMaterialResources(deviceResources, materialEntry.Value);
        }

        var drawItems = new List<RenderDrawItem>(scene.Meshes.Count);
        foreach (var mesh in scene.Meshes)
        {
            var meshResources = CreateMeshResources(deviceResources, mesh);
            _meshes.Add(meshResources);

            var materialResources = ResolveMaterialResources(deviceResources, mesh.MaterialName);
            drawItems.Add(new RenderDrawItem
            {
                Mesh = mesh,
                MeshResources = meshResources,
                MaterialResources = materialResources
            });
        }

        DrawItems = drawItems;
    }

    // Updates the GPU resources for the specified material, and updates any draw items that reference it. If the material does not exist in the cache, this method will have no effect.
    public void UpdateMaterial(DeviceResources deviceResources, RenderMaterialSnapshot material)
    {
        ArgumentNullException.ThrowIfNull(deviceResources);
        ArgumentNullException.ThrowIfNull(material);

        if (_materials.TryGetValue(material.Name, out var existingResources))
        {
            existingResources.Dispose();
        }

        var replacementResources = CreateMaterialResources(deviceResources, material);
        _materials[material.Name] = replacementResources;

        if (DrawItems.Count == 0)
        {
            return;
        }

        var updatedDrawItems = new RenderDrawItem[DrawItems.Count];
        for (var i = 0; i < DrawItems.Count; i++)
        {
            var drawItem = DrawItems[i];
            if (!string.Equals(drawItem.Mesh.MaterialName, material.Name, StringComparison.Ordinal))
            {
                updatedDrawItems[i] = drawItem;
                continue;
            }

            updatedDrawItems[i] = new RenderDrawItem
            {
                Mesh = drawItem.Mesh,
                MeshResources = drawItem.MeshResources,
                MaterialResources = replacementResources
            };
        }

        DrawItems = updatedDrawItems;
    }

    public void Dispose()
    {
        Clear();
    }

    private void Clear()
    {
        foreach (var mesh in _meshes)
        {
            mesh.Dispose();
        }

        _meshes.Clear();

        foreach (var material in _materials.Values)
        {
            material.Dispose();
        }

        _materials.Clear();
        DrawItems = [];
    }

    // Creates GPU resources for the provided mesh snapshot, including vertex and index buffers. The caller is responsible for disposing the returned resources when they are no longer needed.
    private static MeshGpuResources CreateMeshResources(DeviceResources deviceResources, RenderMeshSnapshot mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");

        var vertices = BuildVertices(mesh);
        var vertexBuffer = device.CreateBuffer(
            vertices,
            BindFlags.VertexBuffer,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            ResourceOptionFlags.None,
            0,
            0);

        var indexData = mesh.Indices.Count == 0
            ? Array.Empty<uint>()
            : CopyIndices(mesh.Indices);
        var indexBuffer = device.CreateBuffer(
            indexData,
            BindFlags.IndexBuffer,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            ResourceOptionFlags.None,
            0,
            0);

        return new MeshGpuResources
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            IndexCount = (uint)indexData.Length,
            SkinningBuffer = null
        };
    }

    // Creates GPU resources for the provided material snapshot, including a constant buffer for material properties. The caller is responsible for disposing the returned resources when they are no longer needed.
    private static MaterialGpuResources CreateMaterialResources(DeviceResources deviceResources, RenderMaterialSnapshot material)
    {
        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");

        var constants = new PerMaterialConstants
        {
            TintR = material.Tint?.R ?? Color3.White.R,
            TintG = material.Tint?.G ?? Color3.White.G,
            TintB = material.Tint?.B ?? Color3.White.B,
            HasBaseTexture = string.IsNullOrWhiteSpace(material.BaseTexture) ? 0.0f : 1.0f,
            HasNormalMap = string.IsNullOrWhiteSpace(material.BumpMap) ? 0.0f : 1.0f
        };

        var constantBuffer = device.CreateBuffer(
            new[] { constants },
            BindFlags.ConstantBuffer,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            ResourceOptionFlags.None,
            0,
            0);

        return new MaterialGpuResources
        {
            Snapshot = material,
            ShaderKey = BuildShaderKey(material),
            BaseTextureView = null,
            NormalTextureView = null,
            MaterialConstantsBuffer = constantBuffer
        };
    }

    // Resolves the GPU resources for the material with the specified name.
    // If the material cannot be found, this method will attempt to resolve a fallback material. If the fallback material is not found, it will be created with default properties and returned.
    private MaterialGpuResources ResolveMaterialResources(DeviceResources deviceResources, string materialName)
    {
        if (!string.IsNullOrWhiteSpace(materialName)
            && _materials.TryGetValue(materialName, out var materialResources))
        {
            return materialResources;
        }

        const string fallbackMaterialName = "__fallback";
        if (_materials.TryGetValue(fallbackMaterialName, out materialResources))
        {
            return materialResources;
        }

        materialResources = CreateMaterialResources(
            deviceResources,
            new RenderMaterialSnapshot
            {
                Name = fallbackMaterialName
            });

        _materials[fallbackMaterialName] = materialResources;
        return materialResources;
    }

    // Builds an array of ModelVertex structures from the provided mesh snapshot,
    // combining vertex attributes into a single structure for efficient GPU upload.
    // If any vertex attribute is missing or has fewer entries than the vertex count, default values will be used for the missing attributes.
    private static ModelVertex[] BuildVertices(RenderMeshSnapshot mesh)
    {
        var vertexCount = mesh.Vertices.Count;
        var vertices = new ModelVertex[vertexCount];

        for (var i = 0; i < vertexCount; i++)
        {
            var position = mesh.Vertices[i];
            var normal = GetOrDefault(mesh.Normals, i, Vector3.Zero);
            var tangent = GetOrDefault(mesh.Tangents, i, Vector3.Zero);
            var bitangent = GetOrDefault(mesh.BiTangents, i, Vector3.Zero);
            var uv = GetOrDefault(mesh.UVs, i, Vector2.Zero);
            var weights = GetOrDefault(mesh.SkinWeights, i, default);

            vertices[i] = new ModelVertex
            {
                PositionX = position.X,
                PositionY = position.Y,
                PositionZ = position.Z,
                NormalX = normal.X,
                NormalY = normal.Y,
                NormalZ = normal.Z,
                TangentX = tangent.X,
                TangentY = tangent.Y,
                TangentZ = tangent.Z,
                BitangentX = bitangent.X,
                BitangentY = bitangent.Y,
                BitangentZ = bitangent.Z,
                TexCoordX = uv.X,
                TexCoordY = uv.Y,
                BoneIndex0 = weights.BoneIndex0,
                BoneIndex1 = weights.BoneIndex1,
                BoneIndex2 = weights.BoneIndex2,
                BoneIndex3 = weights.BoneIndex3,
                BoneWeight0 = weights.Weight0,
                BoneWeight1 = weights.Weight1,
                BoneWeight2 = weights.Weight2,
                BoneWeight3 = weights.Weight3
            };
        }

        return vertices;
    }

    private static uint[] CopyIndices(IReadOnlyList<uint> indices)
    {
        var copied = new uint[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            copied[i] = indices[i];
        }

        return copied;
    }

    private static T GetOrDefault<T>(IReadOnlyList<T> values, int index, T fallback)
    {
        return index >= 0 && index < values.Count
            ? values[index]
            : fallback;
    }

    // Builds a MaterialShaderKey for the provided material snapshot, determining the appropriate shader features based on the material properties.
    private static MaterialShaderKey BuildShaderKey(RenderMaterialSnapshot material)
    {
        var features = VertexLitGenericFeatures.None;

        if (!string.IsNullOrWhiteSpace(material.BaseTexture))
        {
            features |= VertexLitGenericFeatures.BaseTexture;
        }

        if (!string.IsNullOrWhiteSpace(material.BumpMap))
        {
            features |= VertexLitGenericFeatures.NormalMap;
        }

        if (material.Phong == true)
        {
            features |= VertexLitGenericFeatures.Phong;
        }

        if (material.RimLight == true)
        {
            features |= VertexLitGenericFeatures.RimLight;
        }

        if (material.SelfIllum == true || !string.IsNullOrWhiteSpace(material.EmissiveTexture))
        {
            features |= VertexLitGenericFeatures.SelfIllum;
        }

        if (material.UseEnvMapProbes == true || !string.IsNullOrWhiteSpace(material.EnvMap))
        {
            features |= VertexLitGenericFeatures.EnvMap;
        }

        if (!string.IsNullOrWhiteSpace(material.LightWarpTexture))
        {
            features |= VertexLitGenericFeatures.LightWarp;
        }

        if (material.NoCull == true)
        {
            features |= VertexLitGenericFeatures.DoubleSided;
        }

        var renderMode = MaterialRenderMode.Opaque;
        if (material.Additive == true)
        {
            renderMode = MaterialRenderMode.Additive;
        }
        else if (material.Translucent == true)
        {
            renderMode = MaterialRenderMode.Translucent;
        }
        else if (material.AlphaTest == true)
        {
            renderMode = MaterialRenderMode.AlphaTest;
        }

        return new MaterialShaderKey(
            string.IsNullOrWhiteSpace(material.Shader) ? "VertexLitGeneric" : material.Shader,
            renderMode,
            features);
    }
}
