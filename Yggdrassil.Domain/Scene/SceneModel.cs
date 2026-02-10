using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    public class SceneModel
    {
        public string Name { get; set; } = string.Empty;
        public List<MeshData> Meshes { get; set; } = new List<MeshData>();
        public Bone? RootBone { get; set; } = null; // The root bone of the skeleton hierarchy. This bone will have child bones, which can have their own child bones, forming a tree structure. If the model has no skeleton, this will be null.
    }
}
