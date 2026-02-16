using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;
using Quaternion = Yggdrassil.Domain.Scene.Quaternion;


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
    }
}
