using System;
using System.Numerics;

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

        public override string ToString() => $"({string.Join(", ", _components)})";
    }

    // Convenient aliases for common vector types
    public struct Vector2<T>(T x, T y) where T : struct, INumber<T>
    {
        public T X = x;
        public T Y = y;
        
        public static implicit operator Vector<T>(Vector2<T> v) => new(v.X, v.Y);
        public static readonly Vector2<T> Zero = new(T.CreateChecked(0), T.CreateChecked(0));
        public static readonly Vector2<T> One = new(T.CreateChecked(1), T.CreateChecked(1));
        public override string ToString() => $"({X}, {Y})";
    }

    public struct Vector3<T>(T x, T y, T z) where T : struct, INumber<T>
    {
        public T X = x;
        public T Y = y;
        public T Z = z;
        
        public static implicit operator Vector<T>(Vector3<T> v) => new(v.X, v.Y, v.Z);

        public Vector2<T> xy => new(X, Y);

        public static readonly Vector3<T> Zero = new(T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0));
        public static readonly Vector3<T> One = new(T.CreateChecked(1), T.CreateChecked(1), T.CreateChecked(1));

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    public struct Vector4<T>(T x, T y, T z, T w) where T : struct, INumber<T>
    {
        public T X = x;
        public T Y = y;
        public T Z = z;
        public T W = w;
        
        public static implicit operator Vector<T>(Vector4<T> v) => new(v.X, v.Y, v.Z, v.W);
        
        public Vector3<T> xyz => new(X, Y, Z);
        public Vector2<T> xy => new(X, Y);

        public static readonly Vector4<T> Zero = new(T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0));
        public static readonly Vector4<T> Identity = new(T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(0), T.CreateChecked(1));
        public override string ToString() => $"({X}, {Y}, {Z}, {W})";
    }

}

// Usage examples (add to a separate file or use as needed):
// using Vector2i = Yggdrassil.Domain.Scene.Vector2<int>;
// using Vector3f = Yggdrassil.Domain.Scene.Vector3<float>;
// using Vector4i = Yggdrassil.Domain.Scene.Vector4<int>;
