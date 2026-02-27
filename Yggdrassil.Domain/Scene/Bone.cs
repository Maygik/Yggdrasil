using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;
using Quaternion = Yggdrassil.Domain.Scene.Quaternion;
using Bone = Yggdrassil.Domain.Scene.Bone;


namespace Yggdrassil.Domain.Scene
{
    /// <summary>
    /// Represents a single bone in a skeleton hierarchy.
    /// Includes transformation data and parent-child relationships.
    /// Does not perform animation or transform logic.
    /// </summary>
    public class Bone : Transform
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDeform { get; set; } = true; // Whether this bone has a direct influence on the mesh.

        // Parameterless constructor for JSON deserialization
        public Bone() : base()
        {
        }

        public Bone? FindBoneInChildren(string name)
        {
            foreach (var child in Children)
            {
                if (child is Bone childBone)
                {
                    if (childBone.Name == name)
                        return childBone;
                    var found = childBone.FindBoneInChildren(name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        public void PrintBoneHierarchy(string indent = "")
        {
            Console.WriteLine($"{indent}{Name}: {LocalPosition} {LocalRotation}");
            foreach (var child in Children)
            {
                if (child is Bone childBone)
                    childBone.PrintBoneHierarchy(indent + "  ");
            }
        }

        public List<Bone> GetAllDescendantsAndSelf()
        {
            List<Bone> result = new List<Bone>();
            void Traverse(Bone bone)
            {
                result.Add(bone);
                foreach (var child in bone.Children)
                {
                    if (child is Bone childBone)
                    {
                        Traverse(childBone);
                    }
                }
            }
            Traverse(this);
            return result;
        }

        /// <summary>
        /// Creates a deep copy of this bone and all its descendants
        /// </summary>
        public Bone DeepClone()
        {
            var clone = new Bone(Name)
            {
                IsDeform = IsDeform,
                LocalMatrix = new Matrix4x4(LocalMatrix.M) // Copy the matrix array
            };

            // Recursively clone all children
            foreach (var child in Children)
            {
                if (child is Bone childBone)
                {
                    clone.AddChild(childBone.DeepClone());
                }
            }

            return clone;
        }

        public Bone(string name, Vector3 position, Quaternion rotation, Vector3 scale) : base(position, rotation, scale)
        {
            Name = name;
        }

        // Explicit constructor with optional parameters for position, rotation and scale
        public Bone(string name, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null) 
            : base(position ?? Vector3.Zero, rotation ?? new Quaternion(), scale ?? Vector3.One)
        {
            Name = name;
        }

        public Bone(string name) : base()
        {
            Name = name;
        }

        public Bone(string name, Matrix4x4 matrix) : base(matrix)
        {
            Name = name;
        }
    }
}
