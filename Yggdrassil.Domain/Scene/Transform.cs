using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;

namespace Yggdrassil.Domain.Scene
{
    public class Transform
    {
        public Transform? Parent { get; set; }
        public List<Transform> Children { get; set; } = new List<Transform>();

        public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            LocalMatrix = Matrix4x4.Identity();

            var r = rotation.ToMatrix();

            foreach (var i in Enumerable.Range(0, 3))
            {
                LocalMatrix.M[i, 0] = r.M[i, 0] * scale.X;
                LocalMatrix.M[i, 1] = r.M[i, 1] * scale.Y;
                LocalMatrix.M[i, 2] = r.M[i, 2] * scale.Z;
            }

            r.M[0, 3] = position.X;
            r.M[1, 3] = position.Y;
            r.M[2, 3] = position.Z;

            LocalMatrix = r;
        }

        public Transform()
            : this(Vector3.Zero, new Quaternion(), Vector3.One)
        {
        }

        public void AddChild(Transform child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public Matrix4x4 LocalMatrix { get; set; } = new Matrix4x4();

        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (Parent != null)
                {
                    return Parent.WorldMatrix * LocalMatrix;
                }
                else
                {
                    return LocalMatrix;
                }
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                return new Vector3(LocalMatrix.M[0, 3], LocalMatrix.M[1, 3], LocalMatrix.M[2, 3]);
            }
            set
            {
                LocalMatrix.M[0, 3] = value.X;
                LocalMatrix.M[1, 3] = value.Y;
                LocalMatrix.M[2, 3] = value.Z;
            }
        }
        public Vector3 WorldPosition
        {
            get
            {
                var worldMatrix = WorldMatrix;
                return new Vector3(worldMatrix.M[0, 3], worldMatrix.M[1, 3], worldMatrix.M[2, 3]);
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                return Quaternion.FromMatrix(RemoveScale(LocalMatrix));
            }
            set
            {
                var pos = LocalPosition;
                var scale = LocalScale;

                var r = value.ToMatrix();

                foreach (var i in Enumerable.Range(0, 3))
                {
                    LocalMatrix.M[i, 0] = r.M[i, 0] * scale.X;
                    LocalMatrix.M[i, 1] = r.M[i, 1] * scale.Y;
                    LocalMatrix.M[i, 2] = r.M[i, 2] * scale.Z;
                }

                r.M[0, 3] = pos.X;
                r.M[1, 3] = pos.Y;
                r.M[2, 3] = pos.Z;

                LocalMatrix = r;
            }
        }

        public static Matrix4x4 RemoveScale(Matrix4x4 matrix)
        {
            var scale = matrix.GetScale();
            if (scale.X == 0 || scale.Y == 0 || scale.Z == 0)
            {
                return matrix; // Avoid division by zero
            }

            var r = matrix;
            for (var i = 0; i < 3; i++)
            {
                r.M[i, 0] /= scale.X;
                r.M[i, 1] /= scale.Y;
                r.M[i, 2] /= scale.Z;
            }
            return r;
        }

        public Quaternion WorldRotation
        {
            get
            {
                var worldMatrix = WorldMatrix;
                return Quaternion.FromMatrix(worldMatrix);
            }
        }

        public Vector3 LocalScale
        {
            get
            {
                return LocalMatrix.GetScale();
            }
            set
            {
                LocalMatrix.SetScale(value);
            }
        }

        public Vector3 WorldScale
        {
            get
            {
                var worldMatrix = WorldMatrix;
                return worldMatrix.GetScale();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            // Transform matrix
            sb.AppendLine("Local Matrix:");
            sb.AppendLine(LocalMatrix.ToString());
            // Human readable position, rotation, scale
            sb.AppendLine($"Position: {LocalPosition}");
            sb.AppendLine($"Rotation: {LocalRotation}");
            sb.AppendLine($"Scale: {LocalScale}");

            return sb.ToString();
        }
    }
}
