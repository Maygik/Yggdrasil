using System;
using System.Collections.Generic;

namespace Yggdrasil.Renderer.Scene;

public sealed class RenderSceneSnapshot
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<RenderMeshSnapshot> Meshes { get; init; } = Array.Empty<RenderMeshSnapshot>();

    public IReadOnlyDictionary<string, RenderMaterialSnapshot> Materials { get; init; } = new Dictionary<string, RenderMaterialSnapshot>(StringComparer.Ordinal);

    public RenderSkeletonSnapshot? Skeleton { get; init; }

    public SceneBounds Bounds { get; init; } = new();
}
