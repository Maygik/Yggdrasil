using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    public class Quaternion
    {
        private float _x;
        private float _y;
        private float _z;
        private float _w;

        private bool dirty = true;
        private Vector3 _cached_eulerAngles = new();

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
        public float W
        {
            get
            {
                return _w;
            }
            set
            {
                if (_w != value)
                {
                    _w = value;
                    dirty = true;
                }
            }
        }

        public Quaternion(float w = 0, float x = 0, float y = 0, float z = 0)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
            dirty = true; // Mark as dirty since we have new values.
        }

        public Quaternion(Vector3 vector, float w = 0)
        {
            _x = vector.X;
            _y = vector.Y;
            _z = vector.Z;
            _w = w;
            dirty = true; // Mark as dirty since we have new values.
        }

        public static Quaternion Identity => new(1, 0, 0, 0);
        public static Quaternion Zero => new(0, 0, 0, 0);



        public static Quaternion operator +(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.W + b.W, a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        public static Quaternion operator -(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.W - b.W, a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Quaternion operator *(Quaternion a, float b)
        {
            return new Quaternion(a.W * b, a.X * b, a.Y * b, a.Z * b);
        }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W
            );
        }
        public static Quaternion operator /(Quaternion a, float b)
        {
            return new Quaternion(a.W / b, a.X / b, a.Y / b, a.Z / b);
        }
    }            
}
