using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Matrix4x4 = Yggdrasil.Types.Matrix4x4;
using Vector2 = Yggdrasil.Types.Vector2;
using Vector3 = Yggdrasil.Types.Vector3;

namespace Yggdrasil.Domain.Scene
{
    public class MeshData
    {
        public MeshData() { }

        public string Name { get; set; } = string.Empty; // Name of the mesh, for reference and possibly export.
        public string Material { get; set; } = string.Empty; // Material name for this mesh. This is used during QC generation to assign the correct $cdmaterials path.
        public List<Vector3> Vertices { get; set; } = new List<Vector3>(); // Assuming vertices are stored as Vector3
        public List<Vector3> Normals { get; set; } = new List<Vector3>();  // Assuming normals are stored as Vector3
        public List<Vector2> UVs { get; set; } = new List<Vector2>();      // Assuming UVs are stored as Vector2
        public List<Vector3> Tangents { get; set; } = new List<Vector3>(); // Assuming tangents are stored as Vector3. Just for rendering purposes, not actually used in QC generation.
        public List<Vector3> BiTangents { get; set; } = new List<Vector3>(); // Assuming binormals are stored as Vector3. Just for rendering purposes, not actually used in QC generation.
        public List<List<Tuple<string, float>>> BoneWeights { get; set; } = new List<List<Tuple<string, float>>>(); // List of bone weights for each vertex. Each vertex can be influenced by multiple bones, so we store a list of (bone name, weight) tuples for each vertex.
        public List<Blendshape> Blendshapes { get; set; } = new List<Blendshape>(); // List of blendshapes for this mesh
        
        // .SMD defines each vertex of each face separately, so we can just store faces as tuples of vertex indices.
        public List<Face> Faces { get; set; } = new List<Face>(); 

        /// <summary>
        /// Creates a deep copy of this mesh data
        /// </summary>
        public MeshData DeepClone()
        {
            var clone = new MeshData
            {
                Name = Name,
                Material = Material,
                Vertices = new List<Vector3>(Vertices),
                Normals = new List<Vector3>(Normals),
                UVs = new List<Vector2>(UVs),
                Tangents = new List<Vector3>(Tangents),
                BiTangents = new List<Vector3>(BiTangents),
                BoneWeights = BoneWeights.Select(bw => new List<Tuple<string, float>>(bw)).ToList(),
                Blendshapes = Blendshapes.Select(bs => new Blendshape(bs.Name, new List<int>(bs.VertexIndices), new List<Vector3>(bs.DeltaVertices))).ToList(),
                Faces = Faces.Select(f => new Face(f.Vertex1, f.Vertex2, f.Vertex3)).ToList()
            };
            return clone;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Mesh: {Name}");
            sb.AppendLine($"Vertices: {Vertices.Count}");
            sb.AppendLine($"Normals: {Normals.Count}");
            sb.AppendLine($"Tangents: {Tangents.Count}");
            sb.AppendLine($"BiTangents: {BiTangents.Count}");
            sb.AppendLine($"UVs: {UVs.Count}");
            sb.AppendLine($"BoneWeights: {BoneWeights.Count}");
            sb.AppendLine($"Blendshapes: {Blendshapes.Count}");
            sb.AppendLine($"Faces: {Faces.Count}");
            sb.AppendLine($"Material Name: {Material}");
            return sb.ToString();
        }

        public string ToIndentedString(string indent = "\t")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{indent}Mesh: {Name}");
            sb.AppendLine($"{indent}Vertices: {Vertices.Count}");
            sb.AppendLine($"{indent}Normals: {Normals.Count}");
            sb.AppendLine($"{indent}Tangents: {Tangents.Count}");
            sb.AppendLine($"{indent}BiTangents: {BiTangents.Count}");
            sb.AppendLine($"{indent}UVs: {UVs.Count}");
            sb.AppendLine($"{indent}BoneWeights: {BoneWeights.Count}");
            sb.AppendLine($"{indent}Blendshapes: {Blendshapes.Count}");
            sb.AppendLine($"{indent}Faces: {Faces.Count}");
            sb.AppendLine($"{indent}Material Name: {Material}");
            return sb.ToString();
        }

        public string ToIndentedString(int indentLevel)
        {
            return ToIndentedString(new string('\t', indentLevel));
        }

        public string ToFullString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"------------------------------");
            sb.AppendLine($"Mesh: {Name}");
            sb.AppendLine($"Vertices: {Vertices.Count}");
            for (int i = 0; i < Vertices.Count; i++)
            {
                sb.AppendLine($"\t{i}: {Vertices[i]}");
            }
            sb.AppendLine($"Normals: {Normals.Count}");
            for (int i = 0; i < Normals.Count; i++)
            {
                sb.AppendLine($"\t{i}: {Normals[i]}");
            }
            sb.AppendLine($"UVs: {UVs.Count}");
            for (int i = 0; i < UVs.Count; i++)
            {
                sb.AppendLine($"\t{i}: {UVs[i]}");
            }
            sb.AppendLine($"BoneWeights: {BoneWeights.Count}");
            for (int i = 0; i < BoneWeights.Count; i++)
            {
                sb.AppendLine($"\tVertex {i}:");
                foreach (var bw in BoneWeights[i])
                {
                    sb.AppendLine($"\t\t{bw.Item1}: {bw.Item2}");
                }
            }
            sb.AppendLine($"Faces: {Faces.Count}");
            for (int i = 0; i < Faces.Count; i++)
            {
                var face = Faces[i];
                sb.AppendLine($"\t{i}: ({face.Vertex1}, {face.Vertex2}, {face.Vertex3})");
            }
            sb.AppendLine($"Material Name: {Material}");
            sb.AppendLine($"------------------------------");
            return sb.ToString();
        }

    }

    // A group of meshes
    // Used because Assimp is annoying and separates by material
    // We group them together by node
    public class MeshGroup : Transform
    {
        public string Name { get; set; } = string.Empty;
        public List<MeshData> Meshes { get; set; } = new List<MeshData>();

        /// <summary>
        /// Creates a deep copy of this mesh group
        /// </summary>
        public MeshGroup DeepClone()
        {
            var clone = new MeshGroup
            {
                Name = Name,
                LocalMatrix = new Matrix4x4(LocalMatrix.M),
                Meshes = Meshes.Select(m => m.DeepClone()).ToList()
            };
            return clone;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"MeshGroup: {Name}");

            // Total up the vertices and faces for all meshes in this group
            int totalVertices = Meshes.Sum(m => m.Vertices.Count);
            sb.AppendLine($"Total Vertices: {totalVertices}");

            int totalFaces = Meshes.Sum(m => m.Faces.Count);
            sb.AppendLine($"Total Faces: {totalFaces}");

            List<string> materials = Meshes.Select(m => m.Material).Distinct().ToList();
            sb.AppendLine($"Materials:");
            foreach (var material in materials)
            {
                sb.AppendLine($"\t{material}");
            }



            sb.AppendLine($"Meshes:");
            foreach (var mesh in Meshes)
            {
                sb.AppendLine($"------------------------------");
                sb.AppendLine($"{mesh.ToIndentedString(1)}");
                sb.AppendLine($"------------------------------");
            }
            return sb.ToString();
        }
    }


    // Internal face class, for iterating through vertices securely
    public class Face
    {
        public int Vertex1 { get; set; }
        public int Vertex2 { get; set; }
        public int Vertex3 { get; set; }
        public Face(int v1, int v2, int v3)
        {
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = v3;
        }
        public override string ToString()
        {
            return $"({Vertex1}, {Vertex2}, {Vertex3})";
        }
        public int this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Vertex1,
                    1 => Vertex2,
                    2 => Vertex3,
                    _ => throw new IndexOutOfRangeException("Face only has 3 vertices indexed from 0 to 2.")
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Vertex1 = value;
                        break;
                    case 1:
                        Vertex2 = value;
                        break;
                    case 2:
                        Vertex3 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Face only has 3 vertices indexed from 0 to 2.");
                }
            }
        }

        public static int VertexCount => 3;
    }
}
