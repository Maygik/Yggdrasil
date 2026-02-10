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
    }
}
