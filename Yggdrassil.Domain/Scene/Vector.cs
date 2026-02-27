using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Yggdrassil.Domain.Scene
{
    /// <summary>
    /// Generic vector structure supporting int and float types with variable dimensions.
    /// </summary>
    /// <typeparam name="T">The numeric type (int or float)</typeparam>
    public struct Vector<T> : IEquatable<Vector<T>>
        where T : struct, INumber<T>
    {
        private readonly T[] _components;

        public int Dimensions => _components.Length;

        public T this[int index]
        {
            get => _components[index];
            set => _components[index] = value;
        }

        public Vector(params T[] components)
        {
            _components = new T[components.Length];
            Array.Copy(components, _components, components.Length);
        }

        public Vector(int dimensions)
        {
            _components = new T[dimensions];
        }

        public T X
        {
            get => Dimensions > 0 ? _components[0] : default;
            set { if (Dimensions > 0) _components[0] = value; }
        }

        public T Y
        {
            get => Dimensions > 1 ? _components[1] : default;
            set { if (Dimensions > 1) _components[1] = value; }
        }

        public T Z
        {
            get => Dimensions > 2 ? _components[2] : default;
            set { if (Dimensions > 2) _components[2] = value; }
        }

        public T W
        {
            get => Dimensions > 3 ? _components[3] : default;
            set { if (Dimensions > 3) _components[3] = value; }
        }

        public static Vector<T> operator +(Vector<T> a, Vector<T> b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions");

            var result = new T[a.Dimensions];
            for (int i = 0; i < a.Dimensions; i++)
                result[i] = a[i] + b[i];
            return new Vector<T>(result);
        }

        public static Vector<T> operator -(Vector<T> a, Vector<T> b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions");

            var result = new T[a.Dimensions];
            for (int i = 0; i < a.Dimensions; i++)
                result[i] = a[i] - b[i];
            return new Vector<T>(result);
        }

        public static Vector<T> operator *(Vector<T> v, T scalar)
        {
            var result = new T[v.Dimensions];
            for (int i = 0; i < v.Dimensions; i++)
                result[i] = v[i] * scalar;
            return new Vector<T>(result);
        }

        public bool Equals(Vector<T> other)
        {
            if (Dimensions != other.Dimensions) return false;
            for (int i = 0; i < Dimensions; i++)
                if (!_components[i].Equals(other._components[i]))
                    return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is Vector<T> other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var component in _components)
                hash.Add(component);
            return hash.ToHashCode();
        }

        public T Length()
        {
            T sumOfSquares = T.Zero;
            for (int i = 0; i < Dimensions; i++)
                sumOfSquares += _components[i] * _components[i];
            return T.CreateChecked(MathF.Sqrt(float.CreateChecked(sumOfSquares)));
        }


        public override string ToString() => $"({string.Join(", ", _components)})";
    }

    // Convenient aliases for common vector types
    public struct Vector2<T>(T x, T y) where T : struct, INumber<T>
    {
        public T X = x;
        public T Y = y;

        [JsonIgnore]
        public static Vector2<T> Zero => new(T.CreateChecked(0), T.CreateChecked(0));
        [JsonIgnore]
        public static Vector2<T> One => new(T.CreateChecked(1), T.CreateChecked(1));
        
        public static implicit operator Vector<T>(Vector2<T> v) => new(v.X, v.Y);
        public override string ToString() => $"({X}, {Y})";
    }

    public struct Vector3<T>(T x, T y, T z) where T : struct, INumber<T>
    {
        public T X = x;
        public T Y = y;
        public T Z = z;

        public static implicit operator Vector<T>(Vector3<T> v) => new(v.X, v.Y, v.Z);

        [JsonIgnore]
        public Vector2<T> xy => new(X, Y);

        [JsonIgnore]
        public static Vector3<T> Zero => new(T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0));
        [JsonIgnore]
        public static Vector3<T> One => new(T.CreateChecked(1), T.CreateChecked(1), T.CreateChecked(1));

        public static Vector3<T> operator *(Vector3<T> v, T scalar)
            => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3<T> operator +(Vector3<T> a, Vector3<T> b)
            => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3<T> operator -(Vector3<T> a, Vector3<T> b)
            => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3<T> operator /(Vector3<T> v, T scalar)
            => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

        public static Vector3<T> operator *(Vector3<T> a, Vector3<T> b)
            => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        public override string ToString() => $"({X}, {Y}, {Z})";


        public Vector3<T> Normalised()
        {
            var sum = X * X + Y * Y + Z * Z;
            var length = T.CreateChecked(MathF.Sqrt(float.CreateChecked(sum)));
            if (length == T.Zero)
                return new Vector3<T>(T.Zero, T.Zero, T.Zero);
            return new Vector3<T>(X / length, Y / length, Z / length);
        }

        public T Length()
        {
            var sum = X * X + Y * Y + Z * Z;
            return T.CreateChecked(MathF.Sqrt(float.CreateChecked(sum)));
        }

        public Vector3<T> Cross(Vector3<T> other)
        {
            return new Vector3<T>(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X
            );
        }

        public T Dot(Vector3<T> other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        // Returns the angle in degrees between two vectors
        public static float Angle(Vector3<T> a, Vector3<T> b)
        {
            var dot = a.Dot(b);
            var lengths = a.Length() * b.Length();
            if (lengths == T.Zero)
                return 0f;
            return MathF.Acos(float.CreateChecked(dot / lengths)) * (180f / MathF.PI);
        }
    }

    public struct Vector4<T> where T : struct, INumber<T>
    {
        public T X;
        public T Y;
        public T Z;
        public T W;

        public Vector4(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(T[] components) : this(
            components.Length > 0 ? components[0] : default,
            components.Length > 1 ? components[1] : default,
            components.Length > 2 ? components[2] : default,
            components.Length > 3 ? components[3] : default)
        {
            if (components.Length != 4)
                throw new ArgumentException("Array must have exactly 4 components", nameof(components));
        }

        [JsonIgnore]
        public Vector3<T> xyz => new(X, Y, Z);
        [JsonIgnore]
        public Vector2<T> xy => new(X, Y);

        [JsonIgnore]
        public static Vector4<T> Zero => new(T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0));
        [JsonIgnore]
        public static Vector4<T> Identity => new(T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(1));
        
        public static implicit operator Vector<T>(Vector4<T> v) => new(v.X, v.Y, v.Z, v.W);
        public override string ToString() => $"({X}, {Y}, {Z}, {W})";
    }


    public struct Color4<T>(T r, T g, T b, T a) where T : struct, INumber<T>
    {
        public T R = r;
        public T G = g;
        public T B = b;
        public T A = a;
        public static implicit operator Vector<T>(Color4<T> c) => new(c.R, c.G, c.B, c.A);
        public override string ToString() => $"({R}, {G}, {B}, {A})";

        public static Color4<float> FromInt(int iR, int iG, int iB, int iA)
        {
            return new Color4<float>(iR / 255f, iG / 255f, iB / 255f, iA / 255f);
        }
    }
    public struct Color3<T>(T r, T g, T b) where T : struct, INumber<T>
    {
        public T R = r;
        public T G = g;
        public T B = b;
        public static implicit operator Vector<T>(Color3<T> c) => new(c.R, c.G, c.B);
        public override string ToString() => $"({R}, {G}, {B})";

        public static Color3<float> FromInt(int iR, int iG, int iB)
        {
            return new Color3<float>(iR / 255f, iG / 255f, iB / 255f);
        }
    }
}

// Usage examples (add to a separate file or use as needed):
// using Vector2i = Yggdrassil.Domain.Scene.Vector2<int>;
// using Vector3f = Yggdrassil.Domain.Scene.Vector3<float>;
// using Vector4i = Yggdrassil.Domain.Scene.Vector4<int>;