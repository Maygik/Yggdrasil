using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    public class Vector3
    {
        // Backing fields for X, Y, Z to allow for change tracking and caching of length calculations.
        private float _x;
        private float _y;
        private float _z;

        // Properties for X, Y, Z with change tracking to mark the vector as dirty when any component changes, which is useful for caching calculations like length.
        public float X
        {
            get
            {
                return _x;
            }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    dirty = true;
                }
            }
        }
        public float Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    dirty = true;
                }
            }
        }
        public float Z
        {
            get
            {
                return _z;
            }
            set
            {
                if (_z != value)
                {
                    _z = value;
                    dirty = true;
                }
            }
        }
        // Constructor for easy initialization of the vector.
        public Vector3(float x = 0, float y = 0, float z = 0)
        {
            _x = x;
            _y = y;
            _z = z;
            dirty = true; // Mark as dirty since we have new values.
        }
        // Override ToString for easy debugging and visualization of the vector.
        public override string ToString()
        {
            return $"({_x}, {_y}, {_z})";
        }

        // Caching length for performance, since it can be expensive to calculate and is often used in vector operations.
        private bool dirty = true;
        private float _cached_length = -1;
        public float Length
        {
            get
            {
                if (dirty)
                {
                    _cached_length = MathF.Sqrt(X * X + Y * Y + Z * Z);
                    dirty = false;
                }
                return _cached_length;
            }
        }

        // Cheaper to calculate than Length, and often used in comparisons, so we provide it as well.
        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y + Z * Z;
            }
        }


        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator *(Vector3 a, float scalar)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        public static Vector3 operator *(float scalar, Vector3 a)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        public static Vector3 operator /(Vector3 a, float scalar)
        {
            return new Vector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }


        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }
    }

    public class Vector2
    {
        // Backing fields for X, Y, Z to allow for change tracking and caching of length calculations.
        private float _x;
        private float _y;
        private float _z;

        // Properties for X, Y, Z with change tracking to mark the vector as dirty when any component changes, which is useful for caching calculations like length.
        public float X
        {
            get
            {
                return _x;
            }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    dirty = true;
                }
            }
        }
        public float Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    dirty = true;
                }
            }
        }
        // Constructor for easy initialization of the vector.
        public Vector2(float x = 0, float y = 0)
        {
            _x = x;
            _y = y;
            dirty = true; // Mark as dirty since we have new values.
        }
        // Override ToString for easy debugging and visualization of the vector.
        public override string ToString()
        {
            return $"({_x}, {_y})";
        }

        // Caching length for performance, since it can be expensive to calculate and is often used in vector operations.
        private bool dirty = true;
        private float _cached_length = -1;
        public float Length
        {
            get
            {
                if (dirty)
                {
                    _cached_length = MathF.Sqrt(X * X + Y * Y);
                    dirty = false;
                }
                return _cached_length;
            }
        }

        // Cheaper to calculate than Length, and often used in comparisons, so we provide it as well.
        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }
        public static Vector2 operator *(Vector2 a, float scalar)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }
        public static Vector2 operator *(float scalar, Vector2 a)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }
        public static Vector2 operator /(Vector2 a, float scalar)
        {
            return new Vector2(a.X / scalar, a.Y / scalar);
        }
    }


    public class Vector3i
    {
        // Backing fields for X, Y, Z to allow for change tracking and caching of length calculations.
        private int _x;
        private int _y;
        private int _z;

        // Properties for X, Y, Z with change tracking to mark the vector as dirty when any component changes, which is useful for caching calculations like length.
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    dirty = true;
                }
            }
        }
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    dirty = true;
                }
            }
        }
        public int Z
        {
            get
            {
                return _z;
            }
            set
            {
                if (_z != value)
                {
                    _z = value;
                    dirty = true;
                }
            }
        }
        // Constructor for easy initialization of the vector.
        public Vector3i(int x = 0, int y = 0, int z = 0)
        {
            _x = x;
            _y = y;
            _z = z;
            dirty = true; // Mark as dirty since we have new values.
        }
        // Override ToString for easy debugging and visualization of the vector.
        public override string ToString()
        {
            return $"({_x}, {_y}, {_z})";
        }

        // Caching length for performance, since it can be expensive to calculate and is often used in vector operations.
        private bool dirty = true;
        private float _cached_length = -1;
        public float Length
        {
            get
            {
                if (dirty)
                {
                    _cached_length = MathF.Sqrt((float)(X * X + Y * Y + Z * Z));
                    dirty = false;
                }
                return _cached_length;
            }
        }

        // Cheaper to calculate than Length, and often used in comparisons, so we provide it as well.
        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y + Z * Z;
            }
        }

        public Vector3 Vector3
        {
            get
            {
                return new Vector3(X, Y, Z);
            }
        }

        public static Vector3i operator +(Vector3i a, Vector3i b)
        {
            return new Vector3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        public static Vector3i operator -(Vector3i a, Vector3i b)
        {
            return new Vector3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
        public static Vector3i operator *(Vector3i a, int scalar)
        {
            return new Vector3i(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        public static Vector3i operator *(int scalar, Vector3i a)
        {
            return new Vector3i(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        public static Vector3i operator /(Vector3i a, int scalar)
        {
            return new Vector3i(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }

    }


    public class Vector2i
    {
        // Backing fields for X, Y, Z to allow for change tracking and caching of length calculations.
        private int _x;
        private int _y;
        private int _z;

        // Properties for X, Y, Z with change tracking to mark the vector as dirty when any component changes, which is useful for caching calculations like length.
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    dirty = true;
                }
            }
        }
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    dirty = true;
                }
            }
        }
        // Constructor for easy initialization of the vector.
        public Vector2i(int x = 0, int y = 0)
        {
            _x = x;
            _y = y;
            dirty = true; // Mark as dirty since we have new values.
        }
        // Override ToString for easy debugging and visualization of the vector.
        public override string ToString()
        {
            return $"({_x}, {_y})";
        }

        // Caching length for performance, since it can be expensive to calculate and is often used in vector operations.
        private bool dirty = true;
        private float _cached_length = -1;
        public float Length
        {
            get
            {
                if (dirty)
                {
                    _cached_length = MathF.Sqrt((float)(X * X + Y * Y));
                    dirty = false;
                }
                return _cached_length;
            }
        }

        // Cheaper to calculate than Length, and often used in comparisons, so we provide it as well.
        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        public Vector2 Vector2
        {
            get
            {
                return new Vector2(X, Y);
            }
        }

        public static Vector2i operator +(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.X + b.X, a.Y + b.Y);
        }
        public static Vector2i operator -(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.X - b.X, a.Y - b.Y);
        }
        public static Vector2i operator *(Vector2i a, int scalar)
        {
            return new Vector2i(a.X * scalar, a.Y * scalar);
        }
        public static Vector2i operator *(int scalar, Vector2i a)
        {
            return new Vector2i(a.X * scalar, a.Y * scalar);
        }
        public static Vector2i operator /(Vector2i a, int scalar)
        {
            return new Vector2i(a.X / scalar, a.Y / scalar);
        }
    }


}
