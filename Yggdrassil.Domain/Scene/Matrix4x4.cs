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
