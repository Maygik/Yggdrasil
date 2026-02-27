using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;

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

        public static Quaternion FromMatrix(Matrix4x4 matrix)
        {
            // Implementation based on standard conversion from rotation matrix to quaternion
            // Assumes matrix.M is a 4x4 float[,] with rotation in the upper-left 3x3
            float m00 = matrix.M[0, 0], m01 = matrix.M[0, 1], m02 = matrix.M[0, 2];
            float m10 = matrix.M[1, 0], m11 = matrix.M[1, 1], m12 = matrix.M[1, 2];
            float m20 = matrix.M[2, 0], m21 = matrix.M[2, 1], m22 = matrix.M[2, 2];

            float trace = m00 + m11 + m22;
            float w, x, y, z;

            if (trace > 0.0f)
            {
                float s = (float)Math.Sqrt(trace + 1.0f) * 2f;
                w = 0.25f * s;
                x = (m21 - m12) / s;
                y = (m02 - m20) / s;
                z = (m10 - m01) / s;
            }
            else if ((m00 > m11) && (m00 > m22))
            {
                float s = (float)Math.Sqrt(1.0f + m00 - m11 - m22) * 2f;
                w = (m21 - m12) / s;
                x = 0.25f * s;
                y = (m01 + m10) / s;
                z = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1.0f + m11 - m00 - m22) * 2f;
                w = (m02 - m20) / s;
                x = (m01 + m10) / s;
                y = 0.25f * s;
                z = (m12 + m21) / s;
            }
            else
            {
                float s = (float)Math.Sqrt(1.0f + m22 - m00 - m11) * 2f;
                w = (m10 - m01) / s;
                x = (m02 + m20) / s;
                y = (m12 + m21) / s;
                z = 0.25f * s;
            }

            return new Quaternion(w, x, y, z);
        }

        public Matrix4x4 ToMatrix()
        {
            // Implementation based on standard conversion from quaternion to rotation matrix
            float xx = X * X, yy = Y * Y, zz = Z * Z;
            float xy = X * Y, xz = X * Z, yz = Y * Z;
            float wx = W * X, wy = W * Y, wz = W * Z;
            Matrix4x4 matrix = new Matrix4x4();
            matrix.M[0, 0] = 1f - 2f * (yy + zz);
            matrix.M[0, 1] = 2f * (xy - wz);
            matrix.M[0, 2] = 2f * (xz + wy);
            matrix.M[0, 3] = 0f;
            matrix.M[1, 0] = 2f * (xy + wz);
            matrix.M[1, 1] = 1f - 2f * (xx + zz);
            matrix.M[1, 2] = 2f * (yz - wx);
            matrix.M[1, 3] = 0f;
            matrix.M[2, 0] = 2f * (xz - wy);
            matrix.M[2, 1] = 2f * (yz + wx);
            matrix.M[2, 2] = 1f - 2f * (xx + yy);
            matrix.M[2, 3] = 0f;
            matrix.M[3, 0] = 0f;
            matrix.M[3, 1] = 0f;
            matrix.M[3, 2] = 0f;
            matrix.M[3, 3] = 1f;
            return matrix;
        }

        // Note: Euler angles are typically represented in radians, and the order of rotations can vary (e.g., XYZ, ZYX, etc.). This implementation assumes XYZ order (roll, pitch, yaw).
        public Vector3 EulerAngles
        {
            get
            {
                // Implementation based on standard conversion from quaternion to Euler angles (in radians)
                float ysqr = Y * Y;

                // roll (x-axis rotation)
                float t0 = +2.0f * (W * X + Y * Z);
                float t1 = +1.0f - 2.0f * (X * X + ysqr);
                float roll = (float)Math.Atan2(t0, t1);

                // pitch (y-axis rotation)
                float t2 = +2.0f * (W * Y - Z * X);
                t2 = Math.Clamp(t2, -1.0f, 1.0f);
                float pitch = (float)Math.Asin(t2);

                // yaw (z-axis rotation)
                float t3 = +2.0f * (W * Z + X * Y);
                float t4 = +1.0f - 2.0f * (ysqr + Z * Z);
                float yaw = (float)Math.Atan2(t3, t4);


                return new Vector3(roll, pitch, yaw);
            }
        }

        public static Quaternion FromEulerAngles(float rollRadians, float pitchRadians, float yawRadians)
        {
            return FromEulerAngles(new Vector3(rollRadians, pitchRadians, yawRadians));
        }
        public static Quaternion FromEulerAngles(Vector3 eulerAnglesRadians)
        {
            // Implementation based on standard conversion from Euler angles (in radians) to quaternion
            float cy = (float)Math.Cos(eulerAnglesRadians.Z * 0.5f);
            float sy = (float)Math.Sin(eulerAnglesRadians.Z * 0.5f);
            float cp = (float)Math.Cos(eulerAnglesRadians.Y * 0.5f);
            float sp = (float)Math.Sin(eulerAnglesRadians.Y * 0.5f);
            float cr = (float)Math.Cos(eulerAnglesRadians.X * 0.5f);
            float sr = (float)Math.Sin(eulerAnglesRadians.X * 0.5f);
            return new Quaternion(
                w: cr * cp * cy + sr * sp * sy,
                x: sr * cp * cy - cr * sp * sy,
                y: cr * sp * cy + sr * cp * sy,
                z: cr * cp * sy - sr * sp * cy
            );
        }

        public override string ToString()
        {
            return $"Quaternion(W: {W}, X: {X}, Y: {Y}, Z: {Z})";
        }

        public Quaternion Invert()
        {
            float normSq = W * W + X * X + Y * Y + Z * Z;
            if (normSq <= 1e-8f)
                return Quaternion.Identity;

            return new Quaternion(
                w: W / normSq,
                x: -X / normSq,
                y: -Y / normSq,
                z: -Z / normSq
            );
        }


        // Returns the shortest rotation from "from" to "to". Both "from" and "to" are expected to be normalized vectors.
        public static Quaternion FromToRotation(Vector3<float> from, Vector3<float> to)
        {
            float dot = from.X * to.X + from.Y * to.Y + from.Z * to.Z;
            if (dot > 0.999999f)
            {
                // Vectors are nearly identical, return identity quaternion
                return Identity;
            }
            else if (dot < -0.999999f)
            {
                // Vectors are opposite, find an orthogonal vector for rotation axis
                Vector3<float> orthogonal = Math.Abs(from.X) < 0.1f ? new Vector3<float>(1, 0, 0) : new Vector3<float>(0, 1, 0);
                Vector3<float> axis = new Vector3<float>(
                    from.Y * orthogonal.Z - from.Z * orthogonal.Y,
                    from.Z * orthogonal.X - from.X * orthogonal.Z,
                    from.X * orthogonal.Y - from.Y * orthogonal.X
                );
                float pitch = (float)Math.Atan2(axis.Y, axis.X);
                float roll = (float)Math.Atan2(axis.Z, Math.Sqrt(axis.X * axis.X + axis.Y * axis.Y));
                float yaw = 0; // No yaw rotation needed for 180-degree turn
                return FromEulerAngles(roll, pitch, yaw);
            }
            else
            {
                // General case
                Vector3<float> cross = new Vector3<float>(
                    from.Y * to.Z - from.Z * to.Y,
                    from.Z * to.X - from.X * to.Z,
                    from.X * to.Y - from.Y * to.X
                );
                float s = (float)Math.Sqrt((1 + dot) * 2);
                float invs = 1 / s;
                return new Quaternion(
                    w: s * 0.5f,
                    x: cross.X * invs,
                    y: cross.Y * invs,
                    z: cross.Z * invs
                );
            }
        }


        public Vector3<float> Rotate(Vector3<float> vector)
        {
            // Using the formula: v' = q * v * q^-1
            // For efficiency, we expand this using quaternion components
            float x = this.X;
            float y = this.Y;
            float z = this.Z;
            float w = this.W;

            float vx = vector.X;
            float vy = vector.Y;
            float vz = vector.Z;

            // Calculate qv (quaternion * vector as pure quaternion)
            float ix = w * vx + y * vz - z * vy;
            float iy = w * vy + z * vx - x * vz;
            float iz = w * vz + x * vy - y * vx;
            float iw = -x * vx - y * vy - z * vz;

            // Calculate (qv) * q^-1
            float rx = ix * w + iw * -x + iy * -z - iz * -y;
            float ry = iy * w + iw * -y + iz * -x - ix * -z;
            float rz = iz * w + iw * -z + ix * -y - iy * -x;

            return new Vector3<float>(rx, ry, rz);
        }


        // Creates a quaternion representing a rotation of "angle" degrees around the specified "axis". The "axis" vector is expected to be normalized.
        public static Quaternion FromAngleAxis(float angle, Vector3<float> axis)
        {
            float halfAngle = angle * 0.5f;
            float s = (float)Math.Sin(halfAngle);
            return new Quaternion(
                w: (float)Math.Cos(halfAngle),
                x: axis.X * s,
                y: axis.Y * s,
                z: axis.Z * s
            );
        }
    }
}
