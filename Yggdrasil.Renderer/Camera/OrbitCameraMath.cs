using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsVector3 = System.Numerics.Vector3;

namespace Yggdrasil.Renderer.Camera;

public static class OrbitCameraMath
{
    // Create a view matrix from the camera state
    public static Matrix4x4 CreateViewMatrix(OrbitCameraState cameraState)
    {
        var cameraPosition = CalculateCameraPosition(cameraState);
        var numericsView = NumericsMatrix4x4.CreateLookAt(
            ToNumerics(cameraPosition),
            ToNumerics(cameraState.Target),
            NumericsVector3.UnitZ);
        return FromNumerics(numericsView);
    }

    // Create a projection matrix from the camera state and the aspect ratio of the viewport
    public static Matrix4x4 CreateProjectionMatrix(OrbitCameraState cameraState, float aspectRatio)
    {
        // Convert FOV from degrees to radians
        var fovRadians = MathF.PI * cameraState.FieldOfViewDegrees / 180.0f;

        // Set the near/far plane because I'm too lazy to make them configurable right now
        // Source engine hates super small/big things anyway
        var nearPlane = 0.1f;
        var farPlane = 1000.0f;

        var safeAspectRatio = aspectRatio <= 0.0f ? 1.0f : aspectRatio;
        var numericsProjection = NumericsMatrix4x4.CreatePerspectiveFieldOfView(
            fovRadians,
            safeAspectRatio,
            nearPlane,
            farPlane);
        return FromNumerics(numericsProjection);
    }

    // Create a camera state that frames the given bounds
    public static OrbitCameraState CreateFramedState(SceneBounds bounds)
    {
        var center = bounds.Center;
        var radius = MathF.Max(bounds.Radius, 1.0f);

        // Get the distance needed to frame the bounds based on the FOV
        var fovRadians = MathF.PI * 70.0f / 180.0f;
        var distance = MathF.Max(1.0f, radius / MathF.Sin(fovRadians / 2.0f));
        return new OrbitCameraState
        {
            Target = center,
            Distance = distance,
            YawRadians = MathF.PI / 4.0f,
            PitchRadians = -MathF.PI / 9.0f,
            FieldOfViewDegrees = 70.0f
        };
    }

    public static Vector3 CalculateCameraPosition(OrbitCameraState cameraState)
    {
        var cosPitch = MathF.Cos(cameraState.PitchRadians);
        var sinPitch = MathF.Sin(cameraState.PitchRadians);
        var cosYaw = MathF.Cos(cameraState.YawRadians);
        var sinYaw = MathF.Sin(cameraState.YawRadians);
        var direction = new Vector3(
            cosPitch * cosYaw,
            cosPitch * sinYaw,
            sinPitch).Normalized();
        return cameraState.Target - direction * MathF.Max(cameraState.Distance, 0.01f);
    }

    private static NumericsVector3 ToNumerics(Vector3 vector)
    {
        return new NumericsVector3(vector.X, vector.Y, vector.Z);
    }

    private static Matrix4x4 FromNumerics(NumericsMatrix4x4 matrix)
    {
        return new Matrix4x4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }
}
