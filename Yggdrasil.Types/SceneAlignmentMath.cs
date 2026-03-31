namespace Yggdrasil.Types;

public static class SceneAlignmentMath
{
    private const float DegreesToRadians = MathF.PI / 180.0f;

    // Creates a transformation matrix that combines scaling, rotation, and translation for aligning objects in a scene.
    public static Matrix4x4 CreateAlignmentMatrix(float scale, Vector3 rotationDegrees, Vector3 position)
    {
        var clampedScale = MathF.Max(scale, 0.01f);
        var rotationMatrix = CreateRotationMatrix(rotationDegrees);
        var scaleMatrix = Matrix4x4.CreateUniformScaling(clampedScale);
        var translationMatrix = CreateTranslationMatrix(position);

        return translationMatrix * rotationMatrix * scaleMatrix;
    }

    public static Matrix4x4 CreateTranslationMatrix(Vector3 translation)
    {
        return new Matrix4x4(
            1.0f, 0.0f, 0.0f, translation.X,
            0.0f, 1.0f, 0.0f, translation.Y,
            0.0f, 0.0f, 1.0f, translation.Z,
            0.0f, 0.0f, 0.0f, 1.0f);
    }

    public static Matrix4x4 CreateRotationMatrix(Vector3 rotationDegrees)
    {
        var rotationRadians = rotationDegrees * DegreesToRadians;

        var halfX = rotationRadians.X * 0.5f;
        var halfY = rotationRadians.Y * 0.5f;
        var halfZ = rotationRadians.Z * 0.5f;

        var cx = MathF.Cos(halfX);
        var sx = MathF.Sin(halfX);
        var cy = MathF.Cos(halfY);
        var sy = MathF.Sin(halfY);
        var cz = MathF.Cos(halfZ);
        var sz = MathF.Sin(halfZ);

        var w = (cx * cy * cz) + (sx * sy * sz);
        var x = (sx * cy * cz) - (cx * sy * sz);
        var y = (cx * sy * cz) + (sx * cy * sz);
        var z = (cx * cy * sz) - (sx * sy * cz);

        return CreateRotationMatrix(w, x, y, z);
    }

    public static Vector3 TransformPoint(Matrix4x4 matrix, Vector3 point)
    {
        var transformed = matrix * new Vector4(point.X, point.Y, point.Z, 1.0f);
        if (MathF.Abs(transformed.W) > float.Epsilon && transformed.W != 1.0f)
        {
            return transformed.XYZ / transformed.W;
        }

        return transformed.XYZ;
    }

    public static Vector3 TransformDirection(Matrix4x4 matrix, Vector3 direction)
    {
        return (matrix * new Vector4(direction.X, direction.Y, direction.Z, 0.0f)).XYZ;
    }

    private static Matrix4x4 CreateRotationMatrix(float w, float x, float y, float z)
    {
        var magnitude = MathF.Sqrt((w * w) + (x * x) + (y * y) + (z * z));
        if (magnitude > float.Epsilon)
        {
            w /= magnitude;
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
        }
        else
        {
            w = 1.0f;
            x = 0.0f;
            y = 0.0f;
            z = 0.0f;
        }

        var xx = x * x;
        var yy = y * y;
        var zz = z * z;
        var xy = x * y;
        var xz = x * z;
        var yz = y * z;
        var wx = w * x;
        var wy = w * y;
        var wz = w * z;

        return new Matrix4x4(
            1.0f - (2.0f * (yy + zz)), 2.0f * (xy - wz), 2.0f * (xz + wy), 0.0f,
            2.0f * (xy + wz), 1.0f - (2.0f * (xx + zz)), 2.0f * (yz - wx), 0.0f,
            2.0f * (xz - wy), 2.0f * (yz + wx), 1.0f - (2.0f * (xx + yy)), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);
    }
}
