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

        public Vector3 Position
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
        
        public Quaternion Rotation
        {
            get
            {
                return Quaternion.FromMatrix(LocalMatrix);
            }
            set
            {
                LocalMatrix = value.ToMatrix() * LocalMatrix;
            }
        }

        public Vector3 Scale
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


        public override string ToString()
        {
            var sb = new StringBuilder();

            // Transform matrix
            sb.AppendLine("Local Matrix:");
            sb.AppendLine(LocalMatrix.ToString());
            // Human readable position, rotation, scale
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Rotation: {Rotation}");
            sb.AppendLine($"Scale: {Scale}");

            return sb.ToString();
        }
    }
}
