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
            LocalPosition = position;
            LocalRotation = rotation;
            LocalScale = scale;
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
            set
            {
                // We need to adjust the local position based on the parent's world matrix such that the world position becomes the desired value.
                // This is done by multiplying the desired world position by the inverse of the parent's world matrix.
                if (Parent != null)
                {
                    var parentWorldMatrix = Parent.WorldMatrix;
                    var parentWorldMatrixInverse = parentWorldMatrix.Invert();

                    // This multiplication might need to be the other way around. Just test it and see if it works. If not, swap the order of multiplication.
                    var localPositionHomogeneous = parentWorldMatrixInverse * new Vector4<float>(value.X, value.Y, value.Z, 1);

                    LocalPosition = new Vector3<float>(localPositionHomogeneous.X, localPositionHomogeneous.Y, localPositionHomogeneous.Z);
                }
                else
                {
                    LocalPosition = value; // No parent, so local position is the same as world position
                }
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
                // Set the local rotation
                // Preserve scale and position
                var scale = LocalScale;
                var position = LocalPosition;

                var rotationMatrix = value.ToMatrix();
                LocalMatrix = rotationMatrix;
                LocalMatrix.SetScale(scale);
                LocalPosition = position;
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
                return Quaternion.FromMatrix(RemoveScale(worldMatrix));
            }
            set
            {
                // We need to adjust the local rotation based on the parent's world rotation such that the world rotation becomes the desired value.
                if (Parent != null)
                {
                    var parentWorldRotation = Parent.WorldRotation;
                    var parentWorldRotationInverse = parentWorldRotation.Invert();
                    var localRotation = parentWorldRotationInverse * value;
                    LocalRotation = localRotation;
                }
                else
                {
                    // No parent, so local rotation is the same as world rotation
                    LocalRotation = value; // No parent, so local rotation is the same as world rotation
                }
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
