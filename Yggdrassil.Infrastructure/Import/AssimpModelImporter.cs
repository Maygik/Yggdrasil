using Assimp;
using Assimp.Unmanaged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Scene;
using Bone = Yggdrassil.Domain.Scene.Bone;
using Matrix4x4 = Yggdrassil.Domain.Scene.Matrix4x4;
using Vector2 = Yggdrassil.Domain.Scene.Vector2<float>;
using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;

namespace Yggdrassil.Infrastructure.Import
{
    /// <summary>
    /// Concrete Assimp-based implementation of IModelImporter.
    /// Repsonsible for converting assimp scenes into internal data structures that can be used for exporting to Source engine formats.
    /// </summary>
    public class AssimpModelImporter : IModelImporter
    {
        public static readonly string[] SupportedExtensions = new string[] { ".obj", ".fbx", ".dae", ".glb", ".gltf" };

        // Loads a model from filePath using assimp
        // Converts the assimp scene into a SceneModel, which is an internal representation of the model's geometry
        public Task<SceneModel> ImportModelAsync(string filePath) => Task.FromResult(ImportModelSync(filePath));
        private SceneModel ImportModelSync(string filePath)
        {
            ValidateFilePath(filePath);
            
            Assimp.Scene assimpScene = ImportAssimp(filePath);
            
            Dictionary<string, List<int>> nodeMeshMapping = BuildNodeMeshMapping(assimpScene);
            List<MeshData> meshes = ProcessMeshes(assimpScene);
            Bone? rootBone = BuildSkeletonHierarchy(assimpScene);
            
            SceneModel sceneModel = CreateSceneModel(filePath, nodeMeshMapping, meshes, rootBone, assimpScene);
            
            return sceneModel;
        }

        // Make sure it's a valid filepath and a supported format before we try
        // Throws exceptions if the file is not found or the format is not supported, which can be caught and handled by the caller.
        private void ValidateFilePath(string filePath)
        {
            // Check that file path exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Model file not found at path: {filePath}");
            }
            // Check that file is a supported format: e.g. .obj, .fbx, .dae, etc.
            string extension = Path.GetExtension(filePath).ToLower();
            if (!SupportedExtensions.Contains(extension))
            {
                throw new NotSupportedException($"File format {extension} is not supported. Supported formats are: {string.Join(", ", SupportedExtensions)}");
            }
        }

        // Builds a mapping of node names to the list of mesh indices that are attached.
        // Used to combine meshes together by node, since assimp separates a single "real" mesh into separate meshes
        // on a node by material. Whereas source engine will want combined meshes for bodygroups.
        private Dictionary<string, List<int>> BuildNodeMeshMapping(Assimp.Scene assimpScene)
        {
            Dictionary<string, List<int>> nodeMeshMapping = new Dictionary<string, List<int>>();
            
            void MapMeshes(Assimp.Node parentNode)
            {
                foreach (var node in parentNode.Children)
                {
                    // Only add nodes that actually have meshes attached
                    if (node.MeshCount > 0)
                    {
                        nodeMeshMapping[node.Name] = node.MeshIndices.ToList();
                    }
                    MapMeshes(node);
                }
            }
            
            MapMeshes(assimpScene.RootNode);
            return nodeMeshMapping;
        }

        // Processes all meshes in the assimp scene converting them to the internal structure
        private List<MeshData> ProcessMeshes(Assimp.Scene assimpScene)
        {
            List<MeshData> meshes = new List<MeshData>();
            
            foreach (var mesh in assimpScene.Meshes)
            {
                MeshData meshData = ProcessSingleMesh(mesh, assimpScene);
                meshes.Add(meshData);
            }
            

            return meshes;
        }

        // Converts a single assimp mesh into the internal meshdata structure
        private MeshData ProcessSingleMesh(Assimp.Mesh mesh, Assimp.Scene assimpScene)
        {
            MeshData meshData = new();
            meshData.Name = mesh.Name;
            
            ProcessVertexData(mesh, meshData);
            ProcessFaceData(mesh, meshData, assimpScene);
            ProcessUVData(mesh, meshData);
            ProcessBoneWeights(mesh, meshData);
            
            return meshData;
        }

        // Converts vertex positions, normals, tangents, and bitangents from the assimp mesh to the internal meshdata structure.
        private void ProcessVertexData(Assimp.Mesh mesh, MeshData meshData)
        {
            meshData.Vertices = mesh.Vertices.Select(v => v.ToVector3()).ToList();
            meshData.Normals = mesh.Normals.Select(n => n.ToVector3()).ToList();
            
            if (mesh.HasTangentBasis)
            {
                meshData.Tangents = mesh.Tangents.Select(t => t.ToVector3()).ToList();
                meshData.BiTangents = mesh.BiTangents.Select(bt => bt.ToVector3()).ToList();
            }
            else
            {
                meshData.Tangents = Enumerable.Repeat(new Vector3(0, 0, 0), mesh.VertexCount).ToList();
                meshData.BiTangents = Enumerable.Repeat(new Vector3(0, 0, 0), mesh.VertexCount).ToList();
                Console.WriteLine($"Warning: Mesh {mesh.Name} has no tangents or bitangents. Filling with empty vectors. This may cause issues with viewport rendering.");
            }
        }

        // Converts face indices and material assignments from the assimp mesh to the internal meshdata structure.
        // Ensures triangles, even though assimp should have already triangulated them.
        private void ProcessFaceData(Assimp.Mesh mesh, MeshData meshData, Assimp.Scene assimpScene)
        {
            meshData.Faces = new List<Tuple<int, int, int>>(mesh.FaceCount);
            
            for (int i = 0; i < mesh.FaceCount; i++)
            {
                var face = mesh.Faces[i];
                if (face.IndexCount != 3)
                {
                    throw new Exception($"Non-triangular face found in mesh {mesh.Name}. All faces must be triangles. Face index: {i}");
                }
                meshData.Faces.Add(Tuple.Create(face.Indices[0], face.Indices[1], face.Indices[2]));
            }
            
            meshData.Material = assimpScene.Materials[mesh.MaterialIndex].Name;
        }

        // Converts UV coords from assimp to internal
        // Assigned 0,0 if no UVs are present
        private void ProcessUVData(Assimp.Mesh mesh, MeshData meshData)
        {
            if (mesh.HasTextureCoords(0) && mesh.TextureCoordinateChannels[0].Count > 0)
            {
                meshData.UVs = mesh.TextureCoordinateChannels[0].Select(uv => uv.ToVector2()).ToList();
            }
            else
            {
                meshData.UVs = Enumerable.Repeat(new Vector2(0, 0), mesh.VertexCount).ToList();
                Console.WriteLine($"Warning: Mesh {mesh.Name} has no UVs. Filling with empty UVs. This may cause issues with texturing in source engine formats.");
            }
        }

        // Converts bone weights from assimp to internal structure.
        // Creates a list of bone-weight pairs for each vertex, sorted by weight descending.
        private void ProcessBoneWeights(Assimp.Mesh mesh, MeshData meshData)
        {
            var weightsList = Enumerable.Range(0, mesh.VertexCount)
                                    .Select(_ => new List<Tuple<string, float>>())
                                    .ToList();

            // Populate the weights list with bone-weight pairs for each vertex
            foreach (var bone in mesh.Bones)
            {
                foreach (var weight in bone.VertexWeights)
                {
                    weightsList[weight.VertexID].Add(Tuple.Create(bone.Name, weight.Weight));
                }
            }

            // Sort into descending order and normalize
            foreach (var vertexWeights in weightsList)
            {
                vertexWeights.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                
                // Normalize and limit to 4 weights per vertex
                float totalWeight = vertexWeights.Take(4).Sum(w => w.Item2);
                if (totalWeight > 0)
                {
                    for (int i = 0; i < vertexWeights.Count && i < 4; i++)
                    {
                        vertexWeights[i] = Tuple.Create(vertexWeights[i].Item1, vertexWeights[i].Item2 / totalWeight);
                    }
                    if (vertexWeights.Count > 4)
                    {
                        vertexWeights.RemoveRange(4, vertexWeights.Count - 4);
                    }
                }
            }

            meshData.BoneWeights = weightsList;
        }

        // Builds the skeleton hierarchy from the assimp scene.
        // Surprisingly complex since assimp doesn't differentiate bones
        private Bone? BuildSkeletonHierarchy(Assimp.Scene assimpScene)
        {
            Dictionary<string, Assimp.Node> skeletonNodes = FindSkeletonNodes(assimpScene);
            List<string> rootBoneNames = FindRootBones(skeletonNodes);
    
            ValidateRootBones(rootBoneNames);
    
            if (rootBoneNames.Count == 0)
            {
                Console.WriteLine("Warning: No root bone found in the model. Importing as prop.");
                return null;
            }
    
            return ConvertNodesToBones(skeletonNodes, rootBoneNames[0]);
        }

        // Finds all nodes in the assimp scene that are part of the skeleton.
        // First finds all nodes that are directly referenced by bones in the meshes,
        // then finds any helper nodes that are parents of those nodes
        private Dictionary<string, Assimp.Node> FindSkeletonNodes(Assimp.Scene assimpScene)
        {
            Dictionary<string, Assimp.Node> skeletonNodes = new Dictionary<string, Assimp.Node>();
    
            // Recursively searches for all bones with weight influences
            void FindDeformBones(Assimp.Node parentNode, Assimp.Mesh mesh)
            {
                foreach (var node in parentNode.Children)
                {
                    if (mesh.Bones.Any(b => b.Name == node.Name))
                    {
                        skeletonNodes[node.Name] = node;
                    }
                    FindDeformBones(node, mesh);
                }
            }
            // Start searching for deform bones from each mesh
            foreach (var mesh in assimpScene.Meshes)
            {
                FindDeformBones(assimpScene.RootNode, mesh);
            }

            // Searches for any helper bones that are parents of the deform bones
            // These are bones with no weight influences, but are still important for the skeleton hierarchy
            // Could be dummy bones for positions, or sockets
            // Common in XPS models, where the core limbs have no weight influences, but are still important for the skeleton hierarchy
            var deformBoneNodes = skeletonNodes.Values.ToList();
            foreach (var deformBoneNode in deformBoneNodes)
            {
                for (var n = deformBoneNode; n != null; n = n.Parent)
                {
                    if (!skeletonNodes.ContainsKey(n.Name))
                    {
                        skeletonNodes[n.Name] = n;
                    }
                }
            }

            return skeletonNodes;
        }

        // Finds root bones in the skeleton hierarchy. Root bones are bones that have no parent bone.
        private List<string> FindRootBones(Dictionary<string, Assimp.Node> skeletonNodes)
        {
            List<string> rootBones = new();
    
            foreach (var kvp in skeletonNodes)
            {
                string boneName = kvp.Key;
                Assimp.Node node = kvp.Value;

                // Check if has any parent at all
                if (node.Parent == null)
                {
                    rootBones.Add(boneName);
                    continue;
                }

                // Check if it has a parent, but the parent isn't a bone node
                string parentName = node.Parent.Name;    
                if (!skeletonNodes.ContainsKey(parentName))
                {
                    rootBones.Add(boneName);
                }
            }
    
            return rootBones;
        }

        // Makes sure that there is exactly 0 or 1 root bones in the hierarchy
        // 0 means it can be treated as a prop
        // 1 means it can be treated as a normal model with a skeleton
        private void ValidateRootBones(List<string> rootBones)
        {
            if (rootBones.Count > 1)
            {
                Console.WriteLine("Warning: Multiple root bones found in the model. This can cause issues with exporting to source engine formats. Root bones found:");
                foreach (var boneName in rootBones)
                {
                    Console.WriteLine($"- {boneName}");
                }
                throw new Exception("Multiple root bones found in the model. This can cause issues with exporting to source engine formats. See warning log for details.");
            }
        }

        // Converts the skeleton nodes into the internal bone structure, preserving the hierarchy and local transforms.
        private Bone ConvertNodesToBones(Dictionary<string, Assimp.Node> skeletonNodes, string rootBoneName)
        {
            Assimp.Node rootNode = skeletonNodes[rootBoneName];
            Bone rootBone = new Bone(rootNode.Name);
            rootBone.LocalMatrix = rootNode.Transform.ToMatrix4x4();

            // Recursively convert child nodes that are part of the skeleton into child bones, preserving the hierarchy and local transforms.
            void ConvertChildBones(Assimp.Node parentNode, Bone parentBone)
            {
                foreach (var node in parentNode.Children)
                {
                    if (skeletonNodes.ContainsKey(node.Name))
                    {
                        Bone bone = new Bone(node.Name);
                        bone.LocalMatrix = node.Transform.ToMatrix4x4();
                        parentBone.AddChild(bone);
                        ConvertChildBones(node, bone);
                    }
                }
            }
    
            ConvertChildBones(rootNode, rootBone);
    
            return rootBone;
        }

        // Creates the final SceneModel by combining the node-mesh mapping, processed meshes, and skeleton hierarchy.
        private SceneModel CreateSceneModel(string filePath, Dictionary<string, List<int>> nodeMeshMapping, List<MeshData> meshes, Bone? rootBone, Assimp.Scene assimpScene)
        {
            SceneModel sceneModel = new SceneModel();
            sceneModel.Name = Path.GetFileNameWithoutExtension(filePath);
    
            foreach (var kvp in nodeMeshMapping)
            {
                string nodeName = kvp.Key;
                List<int> meshIndices = kvp.Value;
                
                // Skip nodes with no meshes
                if (meshIndices.Count == 0)
                    continue;
                
                MeshGroup meshGroup = new MeshGroup();
                meshGroup.Name = nodeName;
                // Set the local transform of the mesh group to match the node
                // Else uses normal identity matrix, which *should* be fine
                meshGroup.LocalMatrix = assimpScene.RootNode.FindNode(nodeName)?.Transform.ToMatrix4x4() ?? new Matrix4x4();

                foreach (int meshIndex in meshIndices)
                {
                    meshGroup.Meshes.Add(meshes[meshIndex]);
                }
    
                sceneModel.MeshGroups.Add(meshGroup);
            }
    
            sceneModel.RootBone ??= rootBone;
    
            return sceneModel;
        }

        private Assimp.Scene ImportAssimp(string filePath)
        {

            // Setup the importer context
            AssimpContext context = new AssimpContext();

            // Exclude cameras and lights from the imported scene, as they are not relevant for our purposes
            context.SetConfig(new Assimp.Configs.RemoveComponentConfig(
                Assimp.ExcludeComponent.Cameras |       // Cameras are unused
                Assimp.ExcludeComponent.Lights |        // Lights are unused
                Assimp.ExcludeComponent.Colors          // Colors unusable in source
                ));

            // Actually import the model
            Assimp.Scene assimpScene = context.ImportFile(filePath,
                    PostProcessSteps.GenerateNormals |          // Generate normals if they are missing
                    PostProcessSteps.Triangulate |              // Triangulaate all faces.
                    PostProcessSteps.CalculateTangentSpace      // Calculate tangent and bitangent vectors. Makes normal mapping and phong work properly."
                    );

            return assimpScene;
        }
    }

    public static class AssimpExtensions
    {
        public static Vector3<float> ToVector3(this Assimp.Vector3D v)
        {
            return new Vector3<float>(v.X, v.Y, v.Z);
        }

        public static Vector2<float> ToVector2(this Assimp.Vector3D v)
        {
            return new Vector2<float>(v.X, v.Y);
        }

        public static Vector2<float> ToVector2(this Assimp.Vector2D v)
        {
            return new Vector2<float>(v.X, v.Y);
        }

        public static Matrix4x4 ToMatrix4x4(this Assimp.Matrix4x4 m)
        {
            return new Matrix4x4(
                m.A1, m.A2, m.A3, m.A4,
                m.B1, m.B2, m.B3, m.B4,
                m.C1, m.C2, m.C3, m.C4,
                m.D1, m.D2, m.D3, m.D4
            );
        }
    }
}   
