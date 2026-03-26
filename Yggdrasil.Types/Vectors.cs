using System.Text.Json.Serialization;

namespace Yggdrasil.Types;

public struct Vector2 : IEquatable<Vector2>
{
    public float X { get; set; }

    public float Y { get; set; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    [JsonIgnore]
    public static Vector2 Zero => new(0f, 0f);

    [JsonIgnore]
    public static Vector2 One => new(1f, 1f);

    public float Length() => MathF.Sqrt((X * X) + (Y * Y));

    public float LengthSquared() => (X * X) + (Y * Y);

    public Vector2 Normalized()
    {
        var length = Length();
        return length <= float.Epsilon ? Zero : this / length;
    }

    public Vector2 Normalised() => Normalized();

    public float Dot(Vector2 other) => (X * other.X) + (Y * other.Y);

    public static float Dot(Vector2 left, Vector2 right) => left.Dot(right);

    public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);

    public static Vector2 operator *(Vector2 value, float scalar) => new(value.X * scalar, value.Y * scalar);

    public static Vector2 operator *(float scalar, Vector2 value) => value * scalar;

    public static Vector2 operator /(Vector2 value, float scalar) => new(value.X / scalar, value.Y / scalar);

    public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);

    public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);

    public bool Equals(Vector2 other) => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"({X}, {Y})";
}

public struct Vector2i : IEquatable<Vector2i>
{
    public int X { get; set; }

    public int Y { get; set; }

    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    [JsonIgnore]
    public static Vector2i Zero => new(0, 0);

    [JsonIgnore]
    public static Vector2i One => new(1, 1);

    public static explicit operator Vector2(Vector2i value) => new(value.X, value.Y);

    public static Vector2i operator +(Vector2i left, Vector2i right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector2i operator -(Vector2i left, Vector2i right) => new(left.X - right.X, left.Y - right.Y);

    public static Vector2i operator *(Vector2i value, int scalar) => new(value.X * scalar, value.Y * scalar);

    public static bool operator ==(Vector2i left, Vector2i right) => left.Equals(right);

    public static bool operator !=(Vector2i left, Vector2i right) => !left.Equals(right);

    public bool Equals(Vector2i other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is Vector2i other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"({X}, {Y})";
}

public struct Vector3 : IEquatable<Vector3>
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    [JsonIgnore]
    public Vector2 XY => new(X, Y);

    [JsonIgnore]
    public static Vector3 Zero => new(0f, 0f, 0f);

    [JsonIgnore]
    public static Vector3 One => new(1f, 1f, 1f);

    public float Length() => MathF.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);

    public Vector3 Normalized()
    {
        var length = Length();
        return length <= float.Epsilon ? Zero : this / length;
    }

    public Vector3 Normalised() => Normalized();

    public Vector3 Cross(Vector3 other)
        => new(
            (Y * other.Z) - (Z * other.Y),
            (Z * other.X) - (X * other.Z),
            (X * other.Y) - (Y * other.X));

    public float Dot(Vector3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

    public static float Dot(Vector3 left, Vector3 right) => left.Dot(right);

    public static float Angle(Vector3 left, Vector3 right)
    {
        var lengths = left.Length() * right.Length();
        if (lengths <= float.Epsilon)
        {
            return 0f;
        }

        var cosine = Math.Clamp(left.Dot(right) / lengths, -1f, 1f);
        return MathF.Acos(cosine) * (180f / MathF.PI);
    }

    public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3 operator *(Vector3 value, float scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);

    public static Vector3 operator *(float scalar, Vector3 value) => value * scalar;

    public static Vector3 operator *(Vector3 left, Vector3 right) => new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    public static Vector3 operator /(Vector3 value, float scalar) => new(value.X / scalar, value.Y / scalar, value.Z / scalar);

    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);

    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);

    public bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
}

public struct Vector4 : IEquatable<Vector4>
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public float W { get; set; }

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    [JsonIgnore]
    public Vector3 XYZ => new(X, Y, Z);

    [JsonIgnore]
    public Vector2 XY => new(X, Y);

    [JsonIgnore]
    public static Vector4 Zero => new(0f, 0f, 0f, 0f);

    [JsonIgnore]
    public static Vector4 Identity => new(0f, 0f, 0f, 1f);

    public static Vector4 operator +(Vector4 left, Vector4 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    public static Vector4 operator -(Vector4 left, Vector4 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    public static Vector4 operator *(Vector4 value, float scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar, value.W * scalar);

    public static Vector4 operator *(float scalar, Vector4 value) => value * scalar;

    public static Vector4 operator /(Vector4 value, float scalar) => new(value.X / scalar, value.Y / scalar, value.Z / scalar, value.W / scalar);

    public static bool operator ==(Vector4 left, Vector4 right) => left.Equals(right);

    public static bool operator !=(Vector4 left, Vector4 right) => !left.Equals(right);

    public bool Equals(Vector4 other)
        => X.Equals(other.X)
        && Y.Equals(other.Y)
        && Z.Equals(other.Z)
        && W.Equals(other.W);

    public override bool Equals(object? obj) => obj is Vector4 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}
