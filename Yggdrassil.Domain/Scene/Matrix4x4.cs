using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    // Matrix class for 4x4 transformation matrices, used for storing transforms of nodes in the scene graph. This is a simple wrapper around a 2D array of floats, with some helper methods for common operations like multiplication, inversion, etc.
    public class Matrix4x4
    {
        public float[,] M { get; } = new float[4, 4];

        public Matrix4x4() {  }

        public Matrix4x4(float[,] values)
        {
            if (values.GetLength(0) != 4 || values.GetLength(1) != 4)
            {
                throw new ArgumentException("Input array must be 4x4.");
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    M[i, j] = values[i, j];
                }
            }
        }

        public Matrix4x4(float[] values)
        {
            if (values.Length != 16)
            {
                throw new ArgumentException("Input array must have 16 elements.");
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    M[i, j] = values[i * 4 + j];
                }
            }
        }

        public Matrix4x4(float m00, float m01, float m02, float m03,
                         float m10, float m11, float m12, float m13,
                         float m20, float m21, float m22, float m23,
                         float m30, float m31, float m32, float m33)
        {
            M[0, 0] = m00; M[0, 1] = m01; M[0, 2] = m02; M[0, 3] = m03;
            M[1, 0] = m10; M[1, 1] = m11; M[1, 2] = m12; M[1, 3] = m13;
            M[2, 0] = m20; M[2, 1] = m21; M[2, 2] = m22; M[2, 3] = m23;
            M[3, 0] = m30; M[3, 1] = m31; M[3, 2] = m32; M[3, 3] = m33;
        }

        public float[] GetRow(int row)
        {
            return new float[] { M[row, 0], M[row, 1], M[row, 2], M[row, 3] };
        }
        public float[] GetColumn(int col)
        {
            return new float[] { M[0, col], M[1, col], M[2, col], M[3, col] };
        }


        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.M[i, j] = a.GetRow(i).Zip(b.GetColumn(j), (x, y) => x * y).Sum();
                }
            }
            return result;
        }

        public static Matrix4x4 operator *(Matrix4x4 m, float scalar)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.M[i, j] = m.M[i, j] * scalar;
                }
            }
            return result;
        }

        public static Matrix4x4 operator *(float scalar, Matrix4x4 m)
        {
            return m * scalar;
        }

        public static Matrix4x4 operator +(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.M[i, j] = a.M[i, j] + b.M[i, j];
                }
            }
            return result;
        }
        public static Matrix4x4 operator -(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.M[i, j] = a.M[i, j] - b.M[i, j];
                }
            }
            return result;
        }

        public Matrix4x4 Transpose()
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.M[i, j] = M[j, i];
                }
            }
            return result;
        }

        public static Matrix4x4 Identity()
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                result.M[i, i] = 1.0f;
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                sb.AppendLine(string.Join(", ", GetRow(i)));
            }
            return sb.ToString();
        }



    }
}
