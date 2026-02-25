using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    // Matrix class for 4x4 transformation matrices, used for storing transforms of nodes in the scene graph. This is a simple wrapper around a 2D array of floats, with some helper methods for common operations like multiplication, inversion, etc.
    public class Matrix4x4
    {
        public float[,] M { get; } = new float[4, 4];

        public Matrix4x4() { }

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

        // Indexer properties for matrix elements
        public float M00 { get => M[0, 0]; set => M[0, 0] = value; }
        public float M01 { get => M[0, 1]; set => M[0, 1] = value; }
        public float M02 { get => M[0, 2]; set => M[0, 2] = value; }
        public float M03 { get => M[0, 3]; set => M[0, 3] = value; }
        public float M10 { get => M[1, 0]; set => M[1, 0] = value; }
        public float M11 { get => M[1, 1]; set => M[1, 1] = value; }
        public float M12 { get => M[1, 2]; set => M[1, 2] = value; }
        public float M13 { get => M[1, 3]; set => M[1, 3] = value; }
        public float M20 { get => M[2, 0]; set => M[2, 0] = value; }
        public float M21 { get => M[2, 1]; set => M[2, 1] = value; }
        public float M22 { get => M[2, 2]; set => M[2, 2] = value; }
        public float M23 { get => M[2, 3]; set => M[2, 3] = value; }
        public float M30 { get => M[3, 0]; set => M[3, 0] = value; }
        public float M31 { get => M[3, 1]; set => M[3, 1] = value; }
        public float M32 { get => M[3, 2]; set => M[3, 2] = value; }
        public float M33 { get => M[3, 3]; set => M[3, 3] = value; }

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

        public string ToHumanString()
        {
            StringBuilder sb = new StringBuilder();
            // Format the matrix in a readable way
            // Make sure to align the columns for better readability
            for (int i = 0; i < 4; i++)
            {
                // Format each element to 4 decimal places and align them in columns
                sb.AppendLine(string.Format("{0,10:F4} {1,10:F4} {2,10:F4} {3,10:F4}", M[i, 0], M[i, 1], M[i, 2], M[i, 3]));
            }

            return sb.ToString();
        }


        public Vector3<float> GetScale()
        {
            return new Vector3<float>
            {
                X = (float)Math.Sqrt(M[0, 0] * M[0, 0] + M[1, 0] * M[1, 0] + M[2, 0] * M[2, 0]),
                Y = (float)Math.Sqrt(M[0, 1] * M[0, 1] + M[1, 1] * M[1, 1] + M[2, 1] * M[2, 1]),
                Z = (float)Math.Sqrt(M[0, 2] * M[0, 2] + M[1, 2] * M[1, 2] + M[2, 2] * M[2, 2])
            };
        }


        public static Matrix4x4 CreateScaling(Vector3<float> scale)
        {
            return new Matrix4x4(
                scale.X, 0, 0, 0,
                0, scale.Y, 0, 0,
                0, 0, scale.Z, 0,
                0, 0, 0, 1
            );

        }

        public static Matrix4x4 CreateScaling(float x, float y, float z)
        {
            return CreateScaling(new Vector3<float>(x, y, z));
        }
        public static Matrix4x4 CreateUniformScaling(float scale)
        {
            return CreateScaling(new Vector3<float>(scale, scale, scale));
        }

        public void SetScale(Vector3<float> scale)
        {
            var currentScale = GetScale();
            if (currentScale.X == 0 || currentScale.Y == 0 || currentScale.Z == 0)
            {
                return;
            }

            var scaleFactor = new Vector3<float>(scale.X / currentScale.X, scale.Y / currentScale.Y, scale.Z / currentScale.Z);

            M[0, 0] *= scaleFactor.X; M[1, 0] *= scaleFactor.X; M[2, 0] *= scaleFactor.X;
            M[0, 1] *= scaleFactor.Y; M[1, 1] *= scaleFactor.Y; M[2, 1] *= scaleFactor.Y;
            M[0, 2] *= scaleFactor.Z; M[1, 2] *= scaleFactor.Z; M[2, 2] *= scaleFactor.Z;
        }


        public bool TryInvertAffine(out Matrix4x4 inverse, float epsilon = 1e-8f)
        {
            // Try to invert the matrix assuming it's an affine transform (no perspective). If the matrix is not invertible, return false.
            // We're only really using Matrix for transforms, so it should be fine

            // Yeah this is black magic

            inverse = new Matrix4x4();
            if (Math.Abs(M[3, 0]) > epsilon || Math.Abs(M[3, 1]) > epsilon || Math.Abs(M[3, 2]) > epsilon || Math.Abs(M[3, 3] - 1f) > epsilon)
            {
                return false; // Not an affine matrix
            }

            float det = M[0, 0] * (M[1, 1] * M[2, 2] - M[1, 2] * M[2, 1]) -
                        M[0, 1] * (M[1, 0] * M[2, 2] - M[1, 2] * M[2, 0]) +
                        M[0, 2] * (M[1, 0] * M[2, 1] - M[1, 1] * M[2, 0]);

            if (Math.Abs(det) < epsilon)
            {
                return false; // Not invertible
            }

            float invDet = 1.0f / det;

            // Inverse of the upper-left 3x3 part
            float m00 = (M[1, 1] * M[2, 2] - M[1, 2] * M[2, 1]) * invDet;
            float m01 = (M[0, 2] * M[2, 1] - M[0, 1] * M[2, 2]) * invDet;
            float m02 = (M[0, 1] * M[1, 2] - M[0, 2] * M[1, 1]) * invDet;

            float m10 = (M[1, 2] * M[2, 0] - M[1, 0] * M[2, 2]) * invDet;
            float m11 = (M[0, 0] * M[2, 2] - M[0, 2] * M[2, 0]) * invDet;
            float m12 = (M[0, 2] * M[1, 0] - M[0, 0] * M[1, 2]) * invDet;

            float m20 = (M[1, 0] * M[2, 1] - M[1, 1] * M[2, 0]) * invDet;
            float m21 = (M[0, 1] * M[2, 0] - M[0, 0] * M[2, 1]) * invDet;
            float m22 = (M[0, 0] * M[1, 1] - M[0, 1] * M[1, 0]) * invDet;

            // Translation
            float tx = M[0, 3];
            float ty = M[1, 3];
            float tz = M[2, 3];


            // Inverse translation = -R^-1 * T
            float itx = -(m00 * tx + m01 * ty + m02 * tz);
            float ity = -(m10 * tx + m11 * ty + m12 * tz);
            float itz = -(m20 * tx + m21 * ty + m22 * tz);

            inverse.M[0, 0] = m00; inverse.M[0, 1] = m01; inverse.M[0, 2] = m02; inverse.M[0, 3] = itx;
            inverse.M[1, 0] = m10; inverse.M[1, 1] = m11; inverse.M[1, 2] = m12; inverse.M[1, 3] = ity;
            inverse.M[2, 0] = m20; inverse.M[2, 1] = m21; inverse.M[2, 2] = m22; inverse.M[2, 3] = itz;
            inverse.M[3, 0] = 0; inverse.M[3, 1] = 0; inverse.M[3, 2] = 0; inverse.M[3, 3] = 1;

            return true;
        }

        public Matrix4x4 Invert()
        {
            // Return the inverse of of the matrix

            if (TryInvertAffine(out Matrix4x4 inverse))
            {
                return inverse;
            }
            else
            {
                throw new InvalidOperationException("Matrix is not invertible.");
            }
        }




        // Allow Vector4<float> to be multiplied by Matrix4x4, treating the vector as a row vector (v * M)
        public static Vector4<float> operator *(Vector4<float> v, Matrix4x4 m)
        {
            return new Vector4<float>
            {
                X = v.X * m.M[0, 0] + v.Y * m.M[1, 0] + v.Z * m.M[2, 0] + v.W * m.M[3, 0],
                Y = v.X * m.M[0, 1] + v.Y * m.M[1, 1] + v.Z * m.M[2, 1] + v.W * m.M[3, 1],
                Z = v.X * m.M[0, 2] + v.Y * m.M[1, 2] + v.Z * m.M[2, 2] + v.W * m.M[3, 2],
                W = v.X * m.M[0, 3] + v.Y * m.M[1, 3] + v.Z * m.M[2, 3] + v.W * m.M[3, 3]
            };
        }


        // Allow multiplying the matrix by a Vector4<float> on the right (M * v), treating the vector as a column vector
        public static Vector4<float> operator *(Matrix4x4 m, Vector4<float> v)
        {
            return new Vector4<float>
            {
                X = m.M[0, 0] * v.X + m.M[0, 1] * v.Y + m.M[0, 2] * v.Z + m.M[0, 3] * v.W,
                Y = m.M[1, 0] * v.X + m.M[1, 1] * v.Y + m.M[1, 2] * v.Z + m.M[1, 3] * v.W,
                Z = m.M[2, 0] * v.X + m.M[2, 1] * v.Y + m.M[2, 2] * v.Z + m.M[2, 3] * v.W,
                W = m.M[3, 0] * v.X + m.M[3, 1] * v.Y + m.M[3, 2] * v.Z + m.M[3, 3] * v.W
            };
        }
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
            int index = 0;

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
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    writer.WriteNumberValue(value.M[i, j]);
                }
            }
            writer.WriteEndArray();
        }
    }
}
