using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = Yggdrassil.Types.Vector3;

namespace Yggdrassil.Domain.Scene
{
    public class Blendshape
    {
        public string Name { get; set; } = string.Empty;
        public List<int> VertexIndices { get; set; } = new List<int>(); // The indices of the vertices that are affected by this blendshape. These indices correspond to the vertices in the MeshData.Vertices list.
        public List<Vector3> DeltaVertices { get; set; } = new List<Vector3>(); // The positional changes for each vertex in the blendshape. Corresponds to the VertexIndices list.


        public Blendshape(string name, List<int>? vertexIndices = null, List<Vector3>? deltaVertices = null) 
        {
            Name = name;
            VertexIndices = vertexIndices ?? new List<int>();
            DeltaVertices = deltaVertices ?? new List<Vector3>();
        }
    }
}
