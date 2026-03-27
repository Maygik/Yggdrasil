using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public sealed class OrbitLightState
{
    public static OrbitLightState Default { get; } = CreateDefault();

    public float YawRadians { get; init; }

    public float PitchRadians { get; init; }

    public float AmbientStrength { get; init; } = 0.28f;

    private static OrbitLightState CreateDefault()
    {
        var (yawRadians, pitchRadians) = OrbitRotationMath.CalculateAngles(new Vector3(-0.35f, -0.20f, -0.85f));
        return new OrbitLightState
        {
            YawRadians = yawRadians,
            PitchRadians = pitchRadians,
            AmbientStrength = 0.28f
        };
    }
}
