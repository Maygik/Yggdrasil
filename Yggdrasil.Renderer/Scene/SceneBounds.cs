using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class SceneBounds
{
    public Vector3 Min { get; init; } = Vector3.Zero;

    public Vector3 Max { get; init; } = Vector3.Zero;

    public Vector3 Center => (Min + Max) / 2.0f;

    public float Radius => (Max - Center).Length();
}
