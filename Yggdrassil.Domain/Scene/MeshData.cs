using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;
using Vector2 = Yggdrassil.Domain.Scene.Vector2<float>;

namespace Yggdrassil.Domain.Scene
{
    public class MeshData
    {
        public MeshData() { }

        public string Name { get; set; } = string.Empty; // Name of the mesh, for reference and possibly export.
        public List<Vector3> Vertices { get; set; } = new List<Vector3>(); // Assuming vertices are stored as Vector3
        public List<Vector3> Normals { get; set; } = new List<Vector3>();  // Assuming normals are stored as Vector3
        public List<Vector3> Tangents { get; set; } = new List<Vector3>(); // Assuming tangents are stored as Vector3. Just for rendering purposes, not actually used in QC generation.
        public List<Vector3> BiTangents { get; set; } = new List<Vector3>(); // Assuming binormals are stored as Vector3. Just for rendering purposes, not actually used in QC generation.
        public List<Vector2> UVs { get; set; } = new List<Vector2>();      // Assuming UVs are stored as Vector2
        public List<List<Tuple<string, float>>> BoneWeights { get; set; } = new List<List<Tuple<string, float>>>(); // List of bone weights for each vertex. Each vertex can be influenced by multiple bones, so we store a list of (bone name, weight) tuples for each vertex.
        public List<Blendshape> Blendshapes { get; set; } = new List<Blendshape>(); // List of blendshapes for this mesh
        
        // .SMD defines each vertex of each face separately, so we can just store faces as tuples of vertex indices.
        public List<Tuple<int,int,int>> Faces { get; set; } = new List<Tuple<int,int,int>>(); 
        public List<string> MaterialsByFace { get; set; } = new List<string>(); // List of materials for each face



    }
}
