using System;
using System.Collections.Generic;
using Yggdrasil.Renderer.Graphics.Buffers;
using Yggdrasil.Renderer.Graphics.Shaders;
using Yggdrasil.Renderer.Graphics.Textures;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;
using Vortice.Direct3D11;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class SceneGpuCache : IDisposable
{
    private readonly Dictionary<string, MaterialGpuResources> _materials = new(StringComparer.Ordinal);
    private readonly List<MeshGpuResources> _meshes = [];
    private readonly TextureCache _textureCache = new();
    private BoneDebugPose[] _boneDebugPoses = [];
    private LineListResources? _boneAxisResources;
    private float _cachedBoneAxisLength = float.NaN;
    private int _cachedSelectedBoneIndex = -1;

    public IReadOnlyList<RenderDrawItem> DrawItems { get; private set; } = [];

    public FloorGridResources? FloorResources { get; private set; }

    public FloorGridResources? HeightPlaneResources { get; private set; }

    public LineListResources? BoneConnectionResources { get; private set; }

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

        FloorResources = CreateFloorResources(deviceResources, scene.Bounds);
        HeightPlaneResources = CreateHeightPlaneResources(deviceResources, scene.Bounds);
        LoadBoneDebugResources(deviceResources, scene.Skeleton);

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

    public LineListResources? GetBoneAxisResources(DeviceResources deviceResources, float axisLength, int selectedBoneIndex = -1)
    {
        ArgumentNullException.ThrowIfNull(deviceResources);

        if (_boneDebugPoses.Length == 0)
        {
            return null;
        }

        var clampedAxisLength = MathF.Max(0.01f, axisLength);
        var normalizedSelectedBoneIndex = selectedBoneIndex >= 0 && selectedBoneIndex < _boneDebugPoses.Length
            ? selectedBoneIndex
            : -1;
        if (_boneAxisResources != null
            && MathF.Abs(_cachedBoneAxisLength - clampedAxisLength) <= 0.001f
            && _cachedSelectedBoneIndex == normalizedSelectedBoneIndex)
        {
            return _boneAxisResources;
        }

        _boneAxisResources?.Dispose();
        _boneAxisResources = CreateBoneAxisResources(
            deviceResources,
            _boneDebugPoses,
            clampedAxisLength,
            normalizedSelectedBoneIndex);
        _cachedBoneAxisLength = clampedAxisLength;
        _cachedSelectedBoneIndex = normalizedSelectedBoneIndex;
        return _boneAxisResources;
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
        _textureCache.Clear();
        FloorResources?.Dispose();
        FloorResources = null;
        HeightPlaneResources?.Dispose();
        HeightPlaneResources = null;
        BoneConnectionResources?.Dispose();
        BoneConnectionResources = null;
        _boneAxisResources?.Dispose();
        _boneAxisResources = null;
        _boneDebugPoses = [];
        _cachedBoneAxisLength = float.NaN;
        _cachedSelectedBoneIndex = -1;
        DrawItems = [];
    }

    // Creates GPU resources for the provided mesh snapshot, including vertex and index buffers. The caller is responsible for disposing the returned resources when they are no longer needed.
    private static MeshGpuResources CreateMeshResources(DeviceResources deviceResources, RenderMeshSnapshot mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var vertices = BuildVertices(mesh);
        var indexData = mesh.Indices.Count == 0
            ? Array.Empty<uint>()
            : CopyIndices(mesh.Indices);
        return CreateMeshResources(deviceResources, vertices, indexData);
    }

    private static MeshGpuResources CreateMeshResources(DeviceResources deviceResources, ModelVertex[] vertices, uint[] indexData)
    {
        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");

        var vertexBuffer = device.CreateBuffer(
            vertices,
            BindFlags.VertexBuffer,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            ResourceOptionFlags.None,
            0,
            0);

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

    private static FloorGridResources CreateFloorResources(DeviceResources deviceResources, SceneBounds _)
    {
        const float halfExtent = 48.0f;
        const float floorZ = 0.0f;
        return CreatePlaneResources(deviceResources, Vector3.Zero, halfExtent, floorZ);
    }

    private static FloorGridResources CreateHeightPlaneResources(DeviceResources deviceResources, SceneBounds _)
    {
        const float halfExtent = 12.0f;
        return CreatePlaneResources(deviceResources, Vector3.Zero, halfExtent, 0.0f);
    }

    private static FloorGridResources CreatePlaneResources(DeviceResources deviceResources, Vector3 center, float halfExtent, float z)
    {
        var vertices = new[]
        {
            CreateFloorVertex(center.X - halfExtent, center.Y - halfExtent, z, 0.0f, 1.0f),
            CreateFloorVertex(center.X - halfExtent, center.Y + halfExtent, z, 0.0f, 0.0f),
            CreateFloorVertex(center.X + halfExtent, center.Y + halfExtent, z, 1.0f, 0.0f),
            CreateFloorVertex(center.X + halfExtent, center.Y - halfExtent, z, 1.0f, 1.0f)
        };

        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };

        return new FloorGridResources
        {
            MeshResources = CreateMeshResources(deviceResources, vertices, indices)
        };
    }

    private void LoadBoneDebugResources(DeviceResources deviceResources, RenderSkeletonSnapshot? skeleton)
    {
        BoneConnectionResources?.Dispose();
        BoneConnectionResources = null;

        _boneAxisResources?.Dispose();
        _boneAxisResources = null;
        _cachedBoneAxisLength = float.NaN;
        _cachedSelectedBoneIndex = -1;

        if (skeleton == null || skeleton.Bones.Count == 0)
        {
            _boneDebugPoses = [];
            return;
        }

        _boneDebugPoses = BuildBoneDebugPoses(skeleton);
        BoneConnectionResources = CreateBoneConnectionResources(deviceResources, _boneDebugPoses);
    }

    private static BoneDebugPose[] BuildBoneDebugPoses(RenderSkeletonSnapshot skeleton)
    {
        var poses = new BoneDebugPose[skeleton.Bones.Count];
        var resolvedWorldMatrices = new Matrix4x4?[skeleton.Bones.Count];
        var resolvingWorldMatrices = new bool[skeleton.Bones.Count];

        Matrix4x4 ResolveBoneWorldMatrix(int boneIndex)
        {
            if (resolvedWorldMatrices[boneIndex] is Matrix4x4 cachedWorldMatrix)
            {
                return cachedWorldMatrix;
            }

            if (resolvingWorldMatrices[boneIndex])
            {
                return skeleton.Bones[boneIndex].WorldMatrix.Copy();
            }

            resolvingWorldMatrices[boneIndex] = true;

            var bone = skeleton.Bones[boneIndex];
            var localMatrix = bone.LocalMatrix.Copy();
            Matrix4x4 worldMatrix;

            if (bone.ParentIndex >= 0 && bone.ParentIndex < skeleton.Bones.Count)
            {
                worldMatrix = ResolveBoneWorldMatrix(bone.ParentIndex) * localMatrix;
            }
            else
            {
                worldMatrix = localMatrix;
            }

            resolvingWorldMatrices[boneIndex] = false;
            resolvedWorldMatrices[boneIndex] = worldMatrix;
            return worldMatrix;
        }

        for (var i = 0; i < skeleton.Bones.Count; i++)
        {
            var bone = skeleton.Bones[i];
            var worldMatrix = ResolveBoneWorldMatrix(i);
            poses[i] = new BoneDebugPose(
                TransformPoint(worldMatrix, Vector3.Zero),
                NormalizeOrFallback(worldMatrix.GetXAxis(), new Vector3(1.0f, 0.0f, 0.0f)),
                NormalizeOrFallback(worldMatrix.GetYAxis(), new Vector3(0.0f, 1.0f, 0.0f)),
                NormalizeOrFallback(worldMatrix.GetZAxis(), new Vector3(0.0f, 0.0f, 1.0f)),
                bone.ParentIndex);
        }

        return poses;
    }

    private static LineListResources? CreateBoneConnectionResources(DeviceResources deviceResources, IReadOnlyList<BoneDebugPose> bones)
    {
        if (bones.Count == 0)
        {
            return null;
        }

        const float connectionR = 0.78f;
        const float connectionG = 0.81f;
        const float connectionB = 0.86f;

        var vertices = new List<LineVertex>(bones.Count * 2);
        for (var i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone.ParentIndex < 0 || bone.ParentIndex >= bones.Count)
            {
                continue;
            }

            var parent = bones[bone.ParentIndex];
            vertices.Add(CreateLineVertex(bone.Origin, connectionR, connectionG, connectionB));
            vertices.Add(CreateLineVertex(parent.Origin, connectionR, connectionG, connectionB));
        }

        return CreateLineResources(deviceResources, [.. vertices]);
    }

    private static LineListResources? CreateBoneAxisResources(
        DeviceResources deviceResources,
        IReadOnlyList<BoneDebugPose> bones,
        float axisLength,
        int selectedBoneIndex)
    {
        if (bones.Count == 0)
        {
            return null;
        }

        const float selectedAxisR = 1.0f;
        const float selectedAxisG = 0.84f;
        const float selectedAxisB = 0.22f;

        var vertices = new LineVertex[bones.Count * 6];
        var vertexIndex = 0;

        for (var i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (i == selectedBoneIndex)
            {
                AddAxisLine(vertices, ref vertexIndex, bone.Origin, bone.Origin + (bone.XAxis * axisLength), selectedAxisR, selectedAxisG, selectedAxisB);
                AddAxisLine(vertices, ref vertexIndex, bone.Origin, bone.Origin + (bone.YAxis * axisLength), selectedAxisR, selectedAxisG, selectedAxisB);
                AddAxisLine(vertices, ref vertexIndex, bone.Origin, bone.Origin + (bone.ZAxis * axisLength), selectedAxisR, selectedAxisG, selectedAxisB);
                continue;
            }

            AddAxisLine(vertices, ref vertexIndex, bone.Origin, bone.Origin + (bone.XAxis * axisLength), 1.0f, 0.0f, 0.0f);
            AddAxisLine(vertices, ref vertexIndex, bone.Origin, bone.Origin + (bone.YAxis * axisLength), 0.0f, 1.0f, 0.0f);
            AddAxisLine(vertices, ref vertexIndex, bone.Origin, bone.Origin + (bone.ZAxis * axisLength), 0.0f, 0.0f, 1.0f);
        }

        return CreateLineResources(deviceResources, vertices);
    }

    private static LineListResources? CreateLineResources(DeviceResources deviceResources, LineVertex[] vertices)
    {
        if (vertices.Length == 0)
        {
            return null;
        }

        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");

        var vertexBuffer = device.CreateBuffer(
            vertices,
            BindFlags.VertexBuffer,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            ResourceOptionFlags.None,
            0,
            0);

        return new LineListResources
        {
            VertexBuffer = vertexBuffer,
            VertexCount = (uint)vertices.Length
        };
    }

    // Creates GPU resources for the provided material snapshot, including a constant buffer for material properties. The caller is responsible for disposing the returned resources when they are no longer needed.
    private MaterialGpuResources CreateMaterialResources(DeviceResources deviceResources, RenderMaterialSnapshot material)
    {
        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");

        var baseTextureView = _textureCache.GetTexture(deviceResources, material.BaseTexture);
        var normalTextureView = _textureCache.GetTexture(deviceResources, material.BumpMap);
        var emissiveTextureView = _textureCache.GetTexture(deviceResources, material.EmissiveTexture);
        var lightWarpTextureView = _textureCache.GetTexture(deviceResources, material.LightWarpTexture);
        var envMapTextureView = _textureCache.GetTexture(deviceResources, material.EnvMap);
        var envMapMaskTextureView = _textureCache.GetTexture(deviceResources, material.EnvMapMask);
        var phongExponentTextureView = _textureCache.GetTexture(deviceResources, material.PhongExponentTexture);

        var tint = material.Tint ?? SourceMaterialDefaults.Color2;
        var phongFresnel = material.PhongFresnelRanges ?? SourceMaterialDefaults.PhongFresnelRanges;
        var envMapTint = material.EnvMapTint ?? SourceMaterialDefaults.EnvMapTint;
        var phongTint = material.PhongTint ?? SourceMaterialDefaults.PhongTint;
        var rimLightTint = material.RimLightTint ?? material.PhongTint ?? SourceMaterialDefaults.RimLightTint;

        var constants = new PerMaterialConstants
        {
            TintR = tint.R,
            TintG = tint.G,
            TintB = tint.B,
            HasBaseTexture = baseTextureView is null ? 0.0f : 1.0f,
            HasNormalTexture = normalTextureView is null ? 0.0f : 1.0f,
            HasEmissiveTexture = emissiveTextureView is null ? 0.0f : 1.0f,
            HasLightWarpTexture = lightWarpTextureView is null ? 0.0f : 1.0f,
            HasEnvMapTexture = envMapTextureView is null ? 0.0f : 1.0f,
            HasEnvMapMaskTexture = envMapMaskTextureView is null ? 0.0f : 1.0f,
            HasPhongExponentTexture = phongExponentTextureView is null ? 0.0f : 1.0f,
            HasPhongMaskTexture = 0.0f,
            NoTint = material.NoTint == true ? 1.0f : 0.0f,
            AlphaTest = material.AlphaTest == true ? 1.0f : 0.0f,
            AlphaTestReference = material.AlphaTestReference ?? SourceMaterialDefaults.AlphaTestReference,
            AllowAlphaToCoverage = material.AllowAlphaToCoverage == true ? 1.0f : 0.0f,
            NoCull = material.NoCull == true ? 1.0f : 0.0f,
            Translucent = material.Translucent == true ? 1.0f : 0.0f,
            Additive = material.Additive == true ? 1.0f : 0.0f,
            HalfLambert = material.HalfLambert == true ? 1.0f : 0.0f,
            SelfIllum = material.SelfIllum == true ? 1.0f : 0.0f,
            EmissiveBlendStrength = material.EmissiveBlendStrength ?? SourceMaterialDefaults.EmissiveBlendStrength,
            UseEnvMapProbes = material.UseEnvMapProbes == true ? 1.0f : 0.0f,
            EnvMapContrast = material.EnvMapContrast ?? SourceMaterialDefaults.EnvMapContrast,
            Phong = material.Phong == true ? 1.0f : 0.0f,
            EnvMapTintR = envMapTint.R,
            EnvMapTintG = envMapTint.G,
            EnvMapTintB = envMapTint.B,
            RimLight = material.RimLight == true ? 1.0f : 0.0f,
            PhongBoost = material.PhongBoost ?? SourceMaterialDefaults.PhongBoost,
            PhongExponent = material.PhongExponent ?? SourceMaterialDefaults.PhongExponent,
            RimLightExponent = material.RimLightExponent ?? SourceMaterialDefaults.RimLightExponent,
            RimLightBoost = material.RimLightBoost ?? SourceMaterialDefaults.RimLightBoost,
            PhongFresnelX = phongFresnel.X,
            PhongFresnelY = phongFresnel.Y,
            PhongFresnelZ = phongFresnel.Z,
            Adjusted = material.Adjusted ? 1.0f : 0.0f,
            PhongTintR = phongTint.R,
            PhongTintG = phongTint.G,
            PhongTintB = phongTint.B,
            RimLightTintR = rimLightTint.R,
            RimLightTintG = rimLightTint.G,
            RimLightTintB = rimLightTint.B
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
            BaseTextureView = baseTextureView,
            NormalTextureView = normalTextureView,
            EmissiveTextureView = emissiveTextureView,
            LightWarpTextureView = lightWarpTextureView,
            EnvMapTextureView = envMapTextureView,
            EnvMapMaskTextureView = envMapMaskTextureView,
            PhongExponentTextureView = phongExponentTextureView,
            TextureViews =
            [
                baseTextureView!,
                normalTextureView!,
                emissiveTextureView!,
                lightWarpTextureView!,
                envMapTextureView!,
                envMapMaskTextureView!,
                phongExponentTextureView!,
                null!
            ],
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
            var position = TransformPoint(mesh.ModelMatrix, mesh.Vertices[i]);
            var normal = NormalizeOrFallback(
                TransformDirection(mesh.ModelMatrix, GetOrDefault(mesh.Normals, i, Vector3.Zero)),
                Vector3.Zero);
            var tangent = NormalizeOrFallback(
                TransformDirection(mesh.ModelMatrix, GetOrDefault(mesh.Tangents, i, Vector3.Zero)),
                Vector3.Zero);
            var bitangent = NormalizeOrFallback(
                TransformDirection(mesh.ModelMatrix, GetOrDefault(mesh.BiTangents, i, Vector3.Zero)),
                Vector3.Zero);
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

    private static ModelVertex CreateFloorVertex(float x, float y, float z, float u, float v)
    {
        return new ModelVertex
        {
            PositionX = x,
            PositionY = y,
            PositionZ = z,
            NormalX = 0.0f,
            NormalY = 0.0f,
            NormalZ = 1.0f,
            TangentX = 1.0f,
            TangentY = 0.0f,
            TangentZ = 0.0f,
            BitangentX = 0.0f,
            BitangentY = 1.0f,
            BitangentZ = 0.0f,
            TexCoordX = u,
            TexCoordY = v
        };
    }

    private static void AddAxisLine(
        LineVertex[] vertices,
        ref int vertexIndex,
        Vector3 start,
        Vector3 end,
        float colorR,
        float colorG,
        float colorB)
    {
        vertices[vertexIndex++] = CreateLineVertex(start, colorR, colorG, colorB);
        vertices[vertexIndex++] = CreateLineVertex(end, colorR, colorG, colorB);
    }

    private static LineVertex CreateLineVertex(Vector3 position, float colorR, float colorG, float colorB, float colorA = 1.0f)
    {
        return new LineVertex
        {
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            ColorR = colorR,
            ColorG = colorG,
            ColorB = colorB,
            ColorA = colorA
        };
    }

    private static Vector3 NormalizeOrFallback(Vector3 axis, Vector3 fallback)
    {
        return axis.LengthSquared() <= float.Epsilon
            ? fallback
            : axis.Normalized();
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

    private static Vector3 TransformDirection(Matrix4x4 matrix, Vector3 direction)
    {
        return (matrix * new Vector4(direction.X, direction.Y, direction.Z, 0.0f)).XYZ;
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

    private readonly record struct BoneDebugPose(
        Vector3 Origin,
        Vector3 XAxis,
        Vector3 YAxis,
        Vector3 ZAxis,
        int ParentIndex);
}
