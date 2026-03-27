using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

public static class OrbitRotationMath
{
    public const float DefaultOrbitSensitivity = 0.01f;
    public const float PitchLimitRadians = 1.55f;

    public static (float YawRadians, float PitchRadians) ApplyOrbit(
        float yawRadians,
        float pitchRadians,
        Vector2 pointerDelta,
        float orbitSensitivity = DefaultOrbitSensitivity)
    {
        return (
            yawRadians - (pointerDelta.X * orbitSensitivity),
            ClampPitch(pitchRadians - (pointerDelta.Y * orbitSensitivity)));
    }

    public static float ClampPitch(float pitchRadians)
    {
        return Math.Clamp(pitchRadians, -PitchLimitRadians, PitchLimitRadians);
    }

    public static Vector3 CalculateDirection(float yawRadians, float pitchRadians)
    {
        var clampedPitch = ClampPitch(pitchRadians);
        var cosPitch = MathF.Cos(clampedPitch);
        var sinPitch = MathF.Sin(clampedPitch);
        var cosYaw = MathF.Cos(yawRadians);
        var sinYaw = MathF.Sin(yawRadians);

        return new Vector3(
            cosPitch * cosYaw,
            cosPitch * sinYaw,
            sinPitch).Normalized();
    }

    public static (float YawRadians, float PitchRadians) CalculateAngles(Vector3 direction)
    {
        if (direction.LengthSquared() <= float.Epsilon)
        {
            return (0.0f, 0.0f);
        }

        var normalizedDirection = direction.Normalized();
        return (
            MathF.Atan2(normalizedDirection.Y, normalizedDirection.X),
            ClampPitch(MathF.Asin(Math.Clamp(normalizedDirection.Z, -1.0f, 1.0f))));
    }
}
