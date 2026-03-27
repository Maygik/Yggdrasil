using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class OrbitCameraState
{
    public static OrbitCameraState Default { get; } = new();

    public Vector3 Target { get; init; } = Vector3.Zero;

    public float Distance { get; init; } = 5.0f;

    public float YawRadians { get; init; }

    public float PitchRadians { get; init; }

    public float FieldOfViewDegrees { get; init; } = 70.0f;

}
