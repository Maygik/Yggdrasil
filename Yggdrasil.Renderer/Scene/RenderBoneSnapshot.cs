using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class RenderBoneSnapshot
{
    public string Name { get; init; } = string.Empty;

    public int ParentIndex { get; init; } = -1;

    public bool IsDeform { get; init; } = true;

    public Matrix4x4 LocalMatrix { get; init; } = new();

    public Matrix4x4 WorldMatrix { get; init; } = new();

    public Matrix4x4 InverseBindMatrix { get; init; } = new();
}
