using Assimp;
using Assimp.Unmanaged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Domain.Scene;
using Bone = Yggdrasil.Domain.Scene.Bone;
using Face = Yggdrasil.Domain.Scene.Face;
using Matrix4x4 = Yggdrasil.Types.Matrix4x4;
using Vector2 = Yggdrasil.Types.Vector2;
using Vector3 = Yggdrasil.Types.Vector3;

namespace Yggdrasil.Infrastructure.Import
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

            Console.WriteLine($"Importing model from file: {filePath}");
            Assimp.Scene assimpScene = ImportAssimp(filePath);

            Console.WriteLine($"Model imported successfully. Scene has {assimpScene.MeshCount} meshes and {assimpScene.MaterialCount} materials");

            Console.WriteLine($"Processing scene data...");
            Dictionary<string, List<int>> nodeMeshMapping = BuildNodeMeshMapping(assimpScene);
            Dictionary<int, string> materialKeyMap = BuildMaterialKeyMap(assimpScene);

            Console.WriteLine($"Processing {assimpScene.MeshCount} meshes...");
            List<MeshData> meshes = ProcessMeshes(assimpScene, materialKeyMap);

            Console.WriteLine($"Building skeleton hierarchy...");
            Bone? rootBone = BuildSkeletonHierarchy(assimpScene);
            
            Console.WriteLine($"Creating scene model...");
            SceneModel sceneModel = CreateSceneModel(filePath, nodeMeshMapping, meshes, rootBone, assimpScene);
            
            Console.WriteLine($"Initialising materials...");
            InitialiseMaterials(sceneModel, assimpScene, materialKeyMap);

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
        private List<MeshData> ProcessMeshes(Assimp.Scene assimpScene, IReadOnlyDictionary<int, string> materialKeyMap)
        {
            List<MeshData> meshes = new List<MeshData>();
            
            foreach (var mesh in assimpScene.Meshes)
            {
                MeshData meshData = ProcessSingleMesh(mesh, assimpScene, materialKeyMap);
                meshes.Add(meshData);
            }
            

            return meshes;
        }

        // Converts a single assimp mesh into the internal meshdata structure
        private MeshData ProcessSingleMesh(Assimp.Mesh mesh, Assimp.Scene assimpScene, IReadOnlyDictionary<int, string> materialKeyMap)
        {
            MeshData meshData = new();
            meshData.Name = mesh.Name;

            try
            {
                ProcessVertexData(mesh, meshData);
                ProcessFaceData(mesh, meshData, materialKeyMap);
                ProcessUVData(mesh, meshData);
                ProcessBoneWeights(mesh, meshData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing mesh {mesh.Name}: {ex.Message}");
                throw new Exception($"Error processing mesh {mesh.Name}: {ex.Message}", ex);
            }

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
        private void ProcessFaceData(
            Assimp.Mesh mesh,
            MeshData meshData,
            IReadOnlyDictionary<int, string> materialKeyMap)
        {
            meshData.Faces = new List<Face>(mesh.FaceCount);
            
            for (int i = 0; i < mesh.FaceCount; i++)
            {
                var face = mesh.Faces[i];
                if (face.IndexCount != 3)
                {
                    throw new Exception($"Non-triangular face found in mesh {mesh.Name}. All faces must be triangles. Face index: {i}");
                }
                meshData.Faces.Add(new Face(face.Indices[0], face.Indices[1], face.Indices[2]));
            }
            
            if (!materialKeyMap.TryGetValue(mesh.MaterialIndex, out string? materialKey))
            {
                throw new Exception($"Mesh {mesh.Name} references unknown material index {mesh.MaterialIndex}.");
            }

            meshData.Material = materialKey;
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
            Dictionary<string, Tuple<Assimp.Node, bool>> skeletonNodes = FindSkeletonNodes(assimpScene);

            TrimExtraNodes(assimpScene, skeletonNodes);

            List<string> rootBoneNames = FindRootBones(skeletonNodes);
    
            ValidateRootBones(rootBoneNames);
    
            if (rootBoneNames.Count == 0)
            {
                Console.WriteLine("Warning: No root bone found in the model. Importing as prop.");
                return null;
            }
    

            return ConvertNodesToBones(skeletonNodes, rootBoneNames[0]);
        }


        private void TrimExtraNodes(Assimp.Scene assimpScene, Dictionary<string, Tuple<Assimp.Node, bool>> skeletonNodes)
        {
            bool HasBoneDescendant(Assimp.Node node)
            {
                if (skeletonNodes.ContainsKey(node.Name)) return true;
                return node.Children.Any(HasBoneDescendant);
            }

            List<string> badNodes = new();

            // Trims the extra nodes from the start (e.g. armature, rootnode) until we reach the hips, first deform bone, or first node with multiple children.
            void FindSkeletonRoot(Assimp.Node node)
            {
                var boneChildren = node.Children.Where(c => HasBoneDescendant(c)).ToList();

                if (boneChildren.Count == 1 && !skeletonNodes[node.Name].Item2)
                {
                    badNodes.Add(node.Name);
                    FindSkeletonRoot(boneChildren[0]);  // fixed: boneChildren[0] not Children[0]
                }
            }

            FindSkeletonRoot(assimpScene.RootNode);

            // Delete the bad nodes
            foreach (var badNode in badNodes)
            {
                Console.WriteLine($"Trimming extra node from skeleton: {badNode}");
                skeletonNodes.Remove(badNode);
            }
        }


        // Finds all nodes in the assimp scene that are part of the skeleton.
        // First finds all nodes that are directly referenced by bones in the meshes,
        // then finds any helper nodes that are parents of those nodes
        private Dictionary<string, Tuple<Assimp.Node, bool>> FindSkeletonNodes(Assimp.Scene assimpScene)
        {
            Dictionary<string, Tuple<Assimp.Node, bool>> skeletonNodes = new();

            HashSet<string> deformBoneNames = new();
            foreach (var mesh in assimpScene.Meshes)
            {
                foreach (var bone in mesh.Bones)
                {
                    deformBoneNames.Add(bone.Name);
                }
            }

            // Find the nodes that are directly referenced by bones in the meshes
            void FindNodesByName(Assimp.Node node)
            {
                if (deformBoneNames.Contains(node.Name))
                {
                    skeletonNodes[node.Name] = new Tuple<Assimp.Node, bool>(node, true);
                }
                foreach (var child in node.Children)
                {
                    FindNodesByName(child);
                }
            }

            FindNodesByName(assimpScene.RootNode);


            // Searches for any helper bones that are parents of or in between the deform bones
            // These are bones with no weight influences, but are still important for the skeleton hierarchy
            // Could be dummy bones for positions, or sockets
            // Common in XPS models, where the core limbs have no weight influences, but are still important for the skeleton hierarchy
            var deformBoneNodes = skeletonNodes.Values.ToList();

            foreach (var deformBoneNode in deformBoneNodes)
            {
                for (var n = deformBoneNode.Item1; n != null; n = n.Parent)
                {
                    if (!skeletonNodes.ContainsKey(n.Name))
                    {
                        // Only add if this node has at least one child
                        bool hasSkeletonChild = n.Children.Any(c => skeletonNodes.ContainsKey(c.Name));

                        // Only add helper nodes that look like bones (not "Armature", "Scene", etc.)
                        if (hasSkeletonChild)
                        {
                            skeletonNodes[n.Name] = new Tuple<Assimp.Node, bool>(n, false);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Also search for raw children of the deform bones, since sometimes those are important (like having all fingers)

            void AddChildNodes(Assimp.Node node)
            {
                foreach (var child in node.Children)
                {
                    if (!skeletonNodes.ContainsKey(child.Name))
                    {
                        skeletonNodes[child.Name] = new Tuple<Assimp.Node, bool>(child, false);
                        AddChildNodes(child);
                    }
                }
            }

            foreach (var deformBoneNode in deformBoneNodes)
            {
                AddChildNodes(deformBoneNode.Item1);
            }

            return skeletonNodes;
        }


        // Finds root bones in the skeleton hierarchy. Root bones are bones that have no parent bone.
        private List<string> FindRootBones(Dictionary<string, Tuple<Assimp.Node, bool>> skeletonNodes)
        {
            List<string> rootBones = new();
    
            foreach (var kvp in skeletonNodes)
            {
                string boneName = kvp.Key;
                Assimp.Node node = kvp.Value.Item1;

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
        private Bone ConvertNodesToBones(Dictionary<string, Tuple<Assimp.Node, bool>> skeletonNodes, string rootBoneName)
        {
            Assimp.Node rootNode = skeletonNodes[rootBoneName].Item1;

            Dictionary<string, int> usedBoneNames = new(StringComparer.OrdinalIgnoreCase);

            Bone rootBone = new Bone(GetUniqueBoneName(rootNode.Name, usedBoneNames));

            // Get rootBone's world matrix from the root node
            rootBone.LocalMatrix = rootNode.Transform.ToMatrix4x4();
            // Debug output rootBone transform
            Console.WriteLine($"Root bone: {rootBone.Name}");
            Console.WriteLine($"Local matrix: {rootBone.LocalMatrix.ToHumanString()}");
            Console.WriteLine($"Position: {rootBone.LocalPosition.ToString()}");
            Console.WriteLine($"Rotation: {rootBone.LocalRotation.EulerAngles.ToString()}");
            Console.WriteLine($"Scale: {rootBone.LocalScale.ToString()}");


            // Recursively convert child nodes that are part of the skeleton into child bones, preserving the hierarchy and local transforms.
            void ConvertChildBones(Assimp.Node parentNode, Bone parentBone)
            {
                foreach (var node in parentNode.Children)
                {
                    if (skeletonNodes.ContainsKey(node.Name))
                    {
                        Bone bone = new Bone(GetUniqueBoneName(node.Name, usedBoneNames));
                        bone.LocalMatrix = node.Transform.ToMatrix4x4();
                        parentBone.AddChild(bone);
                        ConvertChildBones(node, bone);
                    }
                }
            }
    
            ConvertChildBones(rootNode, rootBone);
    
            return rootBone;
        }

        private static string GetUniqueBoneName(string baseName, Dictionary<string, int> usedBoneNames)
        {
            if (!usedBoneNames.TryGetValue(baseName, out int duplicateCount))
            {
                usedBoneNames[baseName] = 0;
                return baseName;
            }

            duplicateCount++;
            string uniqueName;
            do
            {
                uniqueName = $"{baseName}_{duplicateCount}";
                duplicateCount++;
            }
            while (usedBoneNames.ContainsKey(uniqueName));

            usedBoneNames[baseName] = duplicateCount - 1;
            usedBoneNames[uniqueName] = 0;
            return uniqueName;
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

        // Creates a stable unique key for every imported material slot.
        // Assimp material names are not guaranteed to be unique or even present, so we cannot use them as identifiers directly.
        private static Dictionary<int, string> BuildMaterialKeyMap(Assimp.Scene assimpScene)
        {
            Dictionary<int, string> materialKeyMap = new();
            HashSet<string> usedKeys = new(StringComparer.OrdinalIgnoreCase);

            for (int materialIndex = 0; materialIndex < assimpScene.MaterialCount; materialIndex++)
            {
                string rawName = assimpScene.Materials[materialIndex].Name?.Trim() ?? string.Empty;
                string baseName = string.IsNullOrWhiteSpace(rawName)
                    ? $"Material_{materialIndex}"
                    : rawName;

                string materialKey = baseName;
                if (!usedKeys.Add(materialKey))
                {
                    materialKey = $"{baseName}_{materialIndex}";
                    int suffix = 1;
                    while (!usedKeys.Add(materialKey))
                    {
                        materialKey = $"{baseName}_{materialIndex}_{suffix++}";
                    }
                }

                materialKeyMap[materialIndex] = materialKey;

                if (!string.Equals(materialKey, rawName, StringComparison.Ordinal))
                {
                    Console.WriteLine(
                        $"Normalizing imported material slot {materialIndex}: '{rawName}' -> '{materialKey}'");
                }
            }

            return materialKeyMap;
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

            // Actually import the model.
            // We intentionally avoid RemoveRedundantMaterials here because the app needs to preserve source material-slot identity,
            // even when multiple slots currently resolve to identical material properties.

            Assimp.Scene assimpScene = context.ImportFile(filePath,
                    PostProcessSteps.GenerateNormals |          // Generate normals if they are missing
                    PostProcessSteps.Triangulate |              // Triangulaate all faces.
                    PostProcessSteps.CalculateTangentSpace      // Calculate tangent and bitangent vectors. Makes normal mapping and phong work properly."
                    );

            // Apply scale to the scene
            // All nodes should have local scale 1
            // Resize vertices and bones by the original local scale, then reset local scale to 1

            void ApplyScaleToNode(Assimp.Node node, Vector3D parentScale)
            {
                node.Transform.Decompose(out var localScaling, out var locRotation, out var locTranslation);
                Vector3D combinedScale = new Vector3D(
                    localScaling.X * parentScale.X,
                    localScaling.Y * parentScale.Y,
                    localScaling.Z * parentScale.Z
                );
                // Apply the combined scale to the vertices of the meshes attached to this node
                if (node.MeshCount > 0)
                {
                    foreach (int meshIndex in node.MeshIndices)
                    {
                        var mesh = assimpScene.Meshes[meshIndex];
                        for (int i = 0; i < mesh.VertexCount; i++)
                        {
                            var vertex = mesh.Vertices[i];
                            vertex.X *= combinedScale.X;
                            vertex.Y *= combinedScale.Y;
                            vertex.Z *= combinedScale.Z;
                            mesh.Vertices[i] = vertex;
                        }

                        foreach (var bone in mesh.Bones)
                        {
                            bone.OffsetMatrix.Decompose(out var boneScale, out var boneRot, out var boneTranslation);

                            // Scale translation by combined scale
                            boneTranslation.X /= combinedScale.X;
                            boneTranslation.Y /= combinedScale.Y;
                            boneTranslation.Z /= combinedScale.Z;

                            // Make the rotation matrix
                            var boneRotMatrix = boneRot.GetMatrix();

                            // Rebuild the bone's matrix without scaling
                            bone.OffsetMatrix = new Assimp.Matrix4x4(
                                boneRotMatrix.A1, boneRotMatrix.A2, boneRotMatrix.A3, boneTranslation.X,
                                boneRotMatrix.B1, boneRotMatrix.B2, boneRotMatrix.B3, boneTranslation.Y,
                                boneRotMatrix.C1, boneRotMatrix.C2, boneRotMatrix.C3, boneTranslation.Z,
                                0, 0, 0, 1
                            );
                        }
                    }
                }
                // Recursively apply to child nodes
                foreach (var child in node.Children)
                {
                    ApplyScaleToNode(child, combinedScale);
                }

                // Reset local scale to 1, but properly scale translation
                var rotMatrix = locRotation.GetMatrix();
                var scaledTranslation = new Vector3D(   locTranslation.X * combinedScale.X,
                                                        locTranslation.Y * combinedScale.Y,
                                                        locTranslation.Z * combinedScale.Z   );

                node.Transform = new Assimp.Matrix4x4(
                    rotMatrix.A1,   rotMatrix.A2,   rotMatrix.A3,   scaledTranslation.X,
                    rotMatrix.B1,   rotMatrix.B2,   rotMatrix.B3,   scaledTranslation.Y,
                    rotMatrix.C1,   rotMatrix.C2,   rotMatrix.C3,   scaledTranslation.Z,
                    0,              0,              0,              1
                );
            }
            ApplyScaleToNode(assimpScene.RootNode, new Vector3D(1,1,1));


            // Normalize scene names so that windows doesn't cry
            // I hate maya, why are there colons everywhere
            // Also stop duplicate names, just add _x suffix

            Dictionary<string, int> nameCounts = new(StringComparer.OrdinalIgnoreCase);

            string SanitizeName(string name)
            {
                // Replace invalid characters with underscores
                char[] invalidChars = Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    name = name.Replace(c, '_');
                }
                return name;
            }
            void NormalizeNodeNames(Assimp.Node node)
            {
                node.Name = SanitizeName(node.Name);

                // If already seen, add a suffix to make it unique
                if (nameCounts.TryGetValue(node.Name, out int count))
                {
                    count++;
                    node.Name = $"{node.Name}_{count}";
                    nameCounts[node.Name] = count;
                }
                // If not seen before, add to the dictionary
                else
                {
                    nameCounts[node.Name] = 1;
                }

                // Recursively normalize child node names
                foreach (var child in node.Children)
                {
                    NormalizeNodeNames(child);
                }
            }

            NormalizeNodeNames(assimpScene.RootNode);



            return assimpScene;
        }

        // Initialises the material settings in the scene model based on the materials actually used by the meshes in the assimp scene.
        // Sometimes assimp imports materials that aren't actually used by any meshes, so we filter those out to avoid confusion.
        // I loveeee models with 7000 materials so my scripts don't work
        private void InitialiseMaterials(
            SceneModel sceneModel,
            Scene assimpScene,
            IReadOnlyDictionary<int, string> materialKeyMap)
        {
            // Collect all material keys actually used by meshes
            HashSet<string> usedMaterialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var meshGroup in sceneModel.MeshGroups)
            {
                foreach (var mesh in meshGroup.Meshes)
                {
                    usedMaterialKeys.Add(mesh.Material);
                }
            }

            // Only add materials that are actually used.
            for (int materialIndex = 0; materialIndex < assimpScene.MaterialCount; materialIndex++)
            {
                if (!materialKeyMap.TryGetValue(materialIndex, out string? materialKey)
                    || !usedMaterialKeys.Contains(materialKey))
                {
                    continue;
                }

                SourceMaterialSettings materialSettings = new SourceMaterialSettings();
                materialSettings.Name = materialKey;
                sceneModel.MaterialSettings[materialKey] = materialSettings;
            }
        }
    }

    public static class AssimpExtensions
    {
        public static Vector3 ToVector3(this Assimp.Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector2 ToVector2(this Assimp.Vector3D v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static Vector2 ToVector2(this Assimp.Vector2D v)
        {
            return new Vector2(v.X, v.Y);
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
