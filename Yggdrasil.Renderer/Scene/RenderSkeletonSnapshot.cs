using System;
using System.Collections.Generic;

namespace Yggdrasil.Renderer.Scene;

public sealed class RenderSkeletonSnapshot
{
    public IReadOnlyList<RenderBoneSnapshot> Bones { get; init; } = Array.Empty<RenderBoneSnapshot>();

    public IReadOnlyDictionary<string, int> BoneIndicesByName { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);
}
