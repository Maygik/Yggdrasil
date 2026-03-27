using System;
using System.Collections.Generic;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class RenderMeshSnapshot
{
    public string Name { get; init; } = string.Empty;

    public string MaterialName { get; init; } = string.Empty;

    public Matrix4x4 ModelMatrix { get; init; } = new();

    public IReadOnlyList<Vector3> Vertices { get; init; } = Array.Empty<Vector3>();

    public IReadOnlyList<Vector3> Normals { get; init; } = Array.Empty<Vector3>();

    public IReadOnlyList<Vector3> Tangents { get; init; } = Array.Empty<Vector3>();

    public IReadOnlyList<Vector3> BiTangents { get; init; } = Array.Empty<Vector3>();

    public IReadOnlyList<Vector2> UVs { get; init; } = Array.Empty<Vector2>();

    public IReadOnlyList<uint> Indices { get; init; } = Array.Empty<uint>();

    public IReadOnlyList<RenderVertexWeights> SkinWeights { get; init; } = Array.Empty<RenderVertexWeights>();
}
