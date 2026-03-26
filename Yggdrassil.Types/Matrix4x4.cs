using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yggdrassil.Types;

[JsonConverter(typeof(Matrix4x4JsonConverter))]
public class Matrix4x4
{
    public float[,] M { get; } = new float[4, 4];

    public Matrix4x4()
    {
        for (var i = 0; i < 4; i++)
        {
            M[i, i] = 1.0f;
        }
    }

    public Matrix4x4(float[,] values)
    {
        if (values.GetLength(0) != 4 || values.GetLength(1) != 4)
        {
            throw new ArgumentException("Input array must be 4x4.", nameof(values));
        }

        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                M[row, column] = values[row, column];
            }
        }
    }

    public Matrix4x4(float[] values)
    {
        if (values.Length != 16)
        {
            throw new ArgumentException("Input array must have 16 elements.", nameof(values));
        }

        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                M[row, column] = values[(row * 4) + column];
            }
        }
    }

    public Matrix4x4(
        float m00, float m01, float m02, float m03,
        float m10, float m11, float m12, float m13,
        float m20, float m21, float m22, float m23,
        float m30, float m31, float m32, float m33)
    {
        M[0, 0] = m00; M[0, 1] = m01; M[0, 2] = m02; M[0, 3] = m03;
        M[1, 0] = m10; M[1, 1] = m11; M[1, 2] = m12; M[1, 3] = m13;
        M[2, 0] = m20; M[2, 1] = m21; M[2, 2] = m22; M[2, 3] = m23;
        M[3, 0] = m30; M[3, 1] = m31; M[3, 2] = m32; M[3, 3] = m33;
    }

    public float this[int row, int column]
    {
        get => M[row, column];
        set => M[row, column] = value;
    }

    [JsonIgnore]
    public float M00 { get => M[0, 0]; set => M[0, 0] = value; }
    [JsonIgnore]
    public float M01 { get => M[0, 1]; set => M[0, 1] = value; }
    [JsonIgnore]
    public float M02 { get => M[0, 2]; set => M[0, 2] = value; }
    [JsonIgnore]
    public float M03 { get => M[0, 3]; set => M[0, 3] = value; }
    [JsonIgnore]
    public float M10 { get => M[1, 0]; set => M[1, 0] = value; }
    [JsonIgnore]
    public float M11 { get => M[1, 1]; set => M[1, 1] = value; }
    [JsonIgnore]
    public float M12 { get => M[1, 2]; set => M[1, 2] = value; }
    [JsonIgnore]
    public float M13 { get => M[1, 3]; set => M[1, 3] = value; }
    [JsonIgnore]
    public float M20 { get => M[2, 0]; set => M[2, 0] = value; }
    [JsonIgnore]
    public float M21 { get => M[2, 1]; set => M[2, 1] = value; }
    [JsonIgnore]
    public float M22 { get => M[2, 2]; set => M[2, 2] = value; }
    [JsonIgnore]
    public float M23 { get => M[2, 3]; set => M[2, 3] = value; }
    [JsonIgnore]
    public float M30 { get => M[3, 0]; set => M[3, 0] = value; }
    [JsonIgnore]
    public float M31 { get => M[3, 1]; set => M[3, 1] = value; }
    [JsonIgnore]
    public float M32 { get => M[3, 2]; set => M[3, 2] = value; }
    [JsonIgnore]
    public float M33 { get => M[3, 3]; set => M[3, 3] = value; }

    [JsonIgnore]
    public static Matrix4x4 Identity => new();

    public Matrix4x4 Copy() => new(M);

    public float[] GetRow(int row) => new[] { M[row, 0], M[row, 1], M[row, 2], M[row, 3] };

    public float[] GetColumn(int column) => new[] { M[0, column], M[1, column], M[2, column], M[3, column] };

    public Vector4 GetRowVector(int row) => new(M[row, 0], M[row, 1], M[row, 2], M[row, 3]);

    public Vector4 GetColumnVector(int column) => new(M[0, column], M[1, column], M[2, column], M[3, column]);

    public Vector3 GetXAxis() => new(M[0, 0], M[1, 0], M[2, 0]);

    public Vector3 GetYAxis() => new(M[0, 1], M[1, 1], M[2, 1]);

    public Vector3 GetZAxis() => new(M[0, 2], M[1, 2], M[2, 2]);

    public static Matrix4x4 operator *(Matrix4x4 left, Matrix4x4 right)
    {
        var result = new Matrix4x4();
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                var value = 0f;
                for (var index = 0; index < 4; index++)
                {
                    value += left.M[row, index] * right.M[index, column];
                }

                result.M[row, column] = value;
            }
        }

        return result;
    }

    public static Matrix4x4 operator *(Matrix4x4 matrix, float scalar)
    {
        var result = new Matrix4x4();
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                result.M[row, column] = matrix.M[row, column] * scalar;
            }
        }

        return result;
    }

    public static Matrix4x4 operator *(float scalar, Matrix4x4 matrix) => matrix * scalar;

    public static Matrix4x4 operator +(Matrix4x4 left, Matrix4x4 right)
    {
        var result = new Matrix4x4();
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                result.M[row, column] = left.M[row, column] + right.M[row, column];
            }
        }

        return result;
    }

    public static Matrix4x4 operator -(Matrix4x4 left, Matrix4x4 right)
    {
        var result = new Matrix4x4();
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                result.M[row, column] = left.M[row, column] - right.M[row, column];
            }
        }

        return result;
    }

    public Matrix4x4 Transpose()
    {
        var result = new Matrix4x4();
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                result.M[row, column] = M[column, row];
            }
        }

        return result;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        for (var row = 0; row < 4; row++)
        {
            builder.AppendLine(string.Join(", ", GetRow(row)));
        }

        return builder.ToString();
    }

    public string ToHumanString()
    {
        var builder = new StringBuilder();
        for (var row = 0; row < 4; row++)
        {
            builder.AppendLine(string.Format("{0,10:F4} {1,10:F4} {2,10:F4} {3,10:F4}", M[row, 0], M[row, 1], M[row, 2], M[row, 3]));
        }

        return builder.ToString();
    }

    public Vector3 GetScale()
    {
        return new Vector3(
            MathF.Sqrt((M[0, 0] * M[0, 0]) + (M[1, 0] * M[1, 0]) + (M[2, 0] * M[2, 0])),
            MathF.Sqrt((M[0, 1] * M[0, 1]) + (M[1, 1] * M[1, 1]) + (M[2, 1] * M[2, 1])),
            MathF.Sqrt((M[0, 2] * M[0, 2]) + (M[1, 2] * M[1, 2]) + (M[2, 2] * M[2, 2])));
    }

    public static Matrix4x4 CreateScaling(Vector3 scale)
        => new(
            scale.X, 0, 0, 0,
            0, scale.Y, 0, 0,
            0, 0, scale.Z, 0,
            0, 0, 0, 1);

    public static Matrix4x4 CreateScaling(float x, float y, float z) => CreateScaling(new Vector3(x, y, z));

    public static Matrix4x4 CreateUniformScaling(float scale) => CreateScaling(new Vector3(scale, scale, scale));

    public void SetScale(Vector3 scale)
    {
        var currentScale = GetScale();
        if (currentScale.X == 0 || currentScale.Y == 0 || currentScale.Z == 0)
        {
            return;
        }

        var scaleFactor = new Vector3(scale.X / currentScale.X, scale.Y / currentScale.Y, scale.Z / currentScale.Z);

        M[0, 0] *= scaleFactor.X; M[1, 0] *= scaleFactor.X; M[2, 0] *= scaleFactor.X;
        M[0, 1] *= scaleFactor.Y; M[1, 1] *= scaleFactor.Y; M[2, 1] *= scaleFactor.Y;
        M[0, 2] *= scaleFactor.Z; M[1, 2] *= scaleFactor.Z; M[2, 2] *= scaleFactor.Z;
    }

    public bool TryInvertAffine(out Matrix4x4 inverse, float epsilon = 1e-8f)
    {
        inverse = new Matrix4x4();
        if (Math.Abs(M[3, 0]) > epsilon || Math.Abs(M[3, 1]) > epsilon || Math.Abs(M[3, 2]) > epsilon || Math.Abs(M[3, 3] - 1f) > epsilon)
        {
            return false;
        }

        var determinant =
            (M[0, 0] * ((M[1, 1] * M[2, 2]) - (M[1, 2] * M[2, 1])))
            - (M[0, 1] * ((M[1, 0] * M[2, 2]) - (M[1, 2] * M[2, 0])))
            + (M[0, 2] * ((M[1, 0] * M[2, 1]) - (M[1, 1] * M[2, 0])));

        if (Math.Abs(determinant) < epsilon)
        {
            return false;
        }

        var inverseDeterminant = 1.0f / determinant;

        var m00 = ((M[1, 1] * M[2, 2]) - (M[1, 2] * M[2, 1])) * inverseDeterminant;
        var m01 = ((M[0, 2] * M[2, 1]) - (M[0, 1] * M[2, 2])) * inverseDeterminant;
        var m02 = ((M[0, 1] * M[1, 2]) - (M[0, 2] * M[1, 1])) * inverseDeterminant;

        var m10 = ((M[1, 2] * M[2, 0]) - (M[1, 0] * M[2, 2])) * inverseDeterminant;
        var m11 = ((M[0, 0] * M[2, 2]) - (M[0, 2] * M[2, 0])) * inverseDeterminant;
        var m12 = ((M[0, 2] * M[1, 0]) - (M[0, 0] * M[1, 2])) * inverseDeterminant;

        var m20 = ((M[1, 0] * M[2, 1]) - (M[1, 1] * M[2, 0])) * inverseDeterminant;
        var m21 = ((M[0, 1] * M[2, 0]) - (M[0, 0] * M[2, 1])) * inverseDeterminant;
        var m22 = ((M[0, 0] * M[1, 1]) - (M[0, 1] * M[1, 0])) * inverseDeterminant;

        var tx = M[0, 3];
        var ty = M[1, 3];
        var tz = M[2, 3];

        var itx = -((m00 * tx) + (m01 * ty) + (m02 * tz));
        var ity = -((m10 * tx) + (m11 * ty) + (m12 * tz));
        var itz = -((m20 * tx) + (m21 * ty) + (m22 * tz));

        inverse.M[0, 0] = m00; inverse.M[0, 1] = m01; inverse.M[0, 2] = m02; inverse.M[0, 3] = itx;
        inverse.M[1, 0] = m10; inverse.M[1, 1] = m11; inverse.M[1, 2] = m12; inverse.M[1, 3] = ity;
        inverse.M[2, 0] = m20; inverse.M[2, 1] = m21; inverse.M[2, 2] = m22; inverse.M[2, 3] = itz;
        inverse.M[3, 0] = 0; inverse.M[3, 1] = 0; inverse.M[3, 2] = 0; inverse.M[3, 3] = 1;

        return true;
    }

    public Matrix4x4 Invert()
    {
        if (TryInvertAffine(out var inverse))
        {
            return inverse;
        }

        throw new InvalidOperationException($"Matrix is not invertible. {ToString()}");
    }

    public static Vector4 operator *(Vector4 vector, Matrix4x4 matrix)
        => new(
            (vector.X * matrix.M[0, 0]) + (vector.Y * matrix.M[1, 0]) + (vector.Z * matrix.M[2, 0]) + (vector.W * matrix.M[3, 0]),
            (vector.X * matrix.M[0, 1]) + (vector.Y * matrix.M[1, 1]) + (vector.Z * matrix.M[2, 1]) + (vector.W * matrix.M[3, 1]),
            (vector.X * matrix.M[0, 2]) + (vector.Y * matrix.M[1, 2]) + (vector.Z * matrix.M[2, 2]) + (vector.W * matrix.M[3, 2]),
            (vector.X * matrix.M[0, 3]) + (vector.Y * matrix.M[1, 3]) + (vector.Z * matrix.M[2, 3]) + (vector.W * matrix.M[3, 3]));

    public static Vector4 operator *(Matrix4x4 matrix, Vector4 vector)
        => new(
            (matrix.M[0, 0] * vector.X) + (matrix.M[0, 1] * vector.Y) + (matrix.M[0, 2] * vector.Z) + (matrix.M[0, 3] * vector.W),
            (matrix.M[1, 0] * vector.X) + (matrix.M[1, 1] * vector.Y) + (matrix.M[1, 2] * vector.Z) + (matrix.M[1, 3] * vector.W),
            (matrix.M[2, 0] * vector.X) + (matrix.M[2, 1] * vector.Y) + (matrix.M[2, 2] * vector.Z) + (matrix.M[2, 3] * vector.W),
            (matrix.M[3, 0] * vector.X) + (matrix.M[3, 1] * vector.Y) + (matrix.M[3, 2] * vector.Z) + (matrix.M[3, 3] * vector.W));
}

public class Matrix4x4JsonConverter : JsonConverter<Matrix4x4>
{
    public override Matrix4x4? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array.");
        }

        var values = new float[16];
        var index = 0;

        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            if (index >= 16)
            {
                throw new JsonException("Expected 16 elements in the array.");
            }

            values[index++] = reader.GetSingle();
            reader.Read();
        }

        if (index != 16)
        {
            throw new JsonException("Expected 16 elements in the array.");
        }

        return new Matrix4x4(values);
    }

    public override void Write(Utf8JsonWriter writer, Matrix4x4 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                writer.WriteNumberValue(value.M[row, column]);
            }
        }

        writer.WriteEndArray();
    }
}
