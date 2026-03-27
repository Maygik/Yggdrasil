using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class ViewportRenderOptions
{
    public static ViewportRenderOptions Default { get; } = new();

    public bool ShowFloor { get; init; } = true;

    public bool ShowHeightPlane { get; init; }

    public bool ShowBones { get; init; }

    public bool ShowBoneConnections { get; init; }

    public float BoneAxisLength { get; init; } = 2.0f;

    public float ModelScale { get; init; } = 1.0f;

    public Vector3 ModelRotationDegrees { get; init; } = Vector3.Zero;

    public Vector3 ModelPosition { get; init; } = Vector3.Zero;

    public float HeightPlaneHeight { get; init; }
}
