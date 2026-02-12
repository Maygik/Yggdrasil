// TODO: FIX THIS SHIT
// It's really horrible to work with assimp
// But I don't want to write my own model importer, so here we are.
// Nightmare nightmare nightmare nightmare



// Overview of how it SHOULD be done instead of this:
// 1. Load the model using assimp, with minimal post-processing (just triangulation and maybe normal generation if needed)
// 2. Traverse the assimp scene graph and build a mapping of node names to their associated meshes and materials.
// This will allow us to preserve the original mesh structure and material assignments from the source model, which is important for bodygroups.
// 3. For each node in the assimp scene graph, merge the meshes associated with that node into a single mesh,
// applying the node's transformation to the vertices. This way we can preserve the original object structure from the source model, while still having a single mesh for each object part.
// 4. For each merged mesh, assign materials to the faces based on the assimp material index, using the mapping we built in step 2.
// 5. Add the merged mesh to the internal scene model's mesh list, which will be used for exporting to source engine formats.



// This results in a clean and organized internal scene representation that closely matches the original source model's structure, while still being suitable for
// exporting to source engine formats. It also avoids the issues caused by assimp's tendency to break up meshes by material, which can lead to a messy and disorganized
// internal representation if not handled properly.







using Assimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Scene;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;
using Vector2 = Yggdrassil.Domain.Scene.Vector2<float>;
using Bone = Yggdrassil.Domain.Scene.Bone;
using Matrix4x4 = Yggdrassil.Domain.Scene.Matrix4x4;

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
        public async Task<SceneModel> ImportModel(string filePath)
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

            Assimp.Scene assimpScene = ImportAssimp(filePath);


            // Convert to internal SceneModel format
            SceneModel internalScene = new();

            // Assimp supports multiple meshes per scene, but each mesh can only have one material.
            // Source engine supports multiple meshes, each with multiple materials.
            // How does assimp store the separate "objects" in a scene?
            // It stores them as separate meshes, but they can be linked together with nodes. Each node can have a transformation and a list of meshes. This allows for hierarchical models with multiple parts.
            // Alright, we need to merge each assimp node into a single mesh, and assign materials to each face based on the assimp material index. This way we can preserve the material assignments while still having a single mesh for the entire model.

            // We need distinct nodes for each object "part" that will become a bodygroup
            // These would normally be separate meshes in something like blender
            // But assimp separates things into independent meshes for every material, which is really annoying to work with.
            // So we need to merge all meshes that were previously part of the same object, then merge all objects that share the same material together, and assign materials to the faces
            // Problem is: how do we determine what used to be together?
            // How does assimp's imported hierarchy look in terms of armature and the REAL meshes, not the weird divided ones?
            // Is it Armature > Real Mesh alongside Armature > Bones
            // Or is it some weird mixture?
            // We need to separate out the bone hierarchy from the real meshes, and then merge the real meshes
            // We CANNOT just merge by material index, because that would merge together meshes that were originally separate objects, which is really bad for bodygroups.
            // We MUST preserve the original mesh structure, before assimp broke everything.

            // Steps:
            // 1. Recursively traverse the assimp scene graph, starting from the root node
            // 2. For each node, merge all meshes associated with that node into a single mesh, applying the node's transformation to the vertices
            // 3. For each merged mesh, assign materials to the faces based on the assimp material index
            // 4. Add the merged mesh to the internal scene model
            // 5. Recursively traverse the child nodes and repeat the process

            // This way we can preserve the original mesh structure, while still having a single mesh for each object part, and correctly assigned materials for bodygroups.

            // List of real meshes we want to use for bodygroups. These are the meshes that were originally separate objects in the source model, before assimp broke them up by material.

            // Populate the bone list by traversing the assimp scene and finding all nodes that are referenced as bones in the meshes

            var rootNode = assimpScene.RootNode;
            Dictionary<string, Bone> boneDictionary = new Dictionary<string, Bone>();
            void TraverseBoneNodes(Assimp.Node node)
            {
                foreach (var child in node.Children)
                {
                    // Check if this node is a bone in any of the meshes
                    bool isBone = assimpScene.Meshes.Any(mesh => mesh.Bones.Any(bone => bone.Name == child.Name));
                    if (isBone)
                    {
                        // If it is a bone, add it to the internal scene model's bone hierarchy
                        Bone internalBone = new()
                        {
                            Name = child.Name,
                            LocalMatrix = child.Transform.ToMatrix4x4() // Convert assimp's 4x4 matrix to our internal format
                        };
                        if (internalScene.RootBone == null)
                        {
                            internalScene.RootBone = internalBone; // If this is the first bone we find, set it as the root bone
                        }
                        else
                        {
                            // Find the parent bone in the internal scene model's bone hierarchy, and add this bone as a child
                            if (boneDictionary.TryGetValue(node.Name, out Bone parentBone))
                            {
                                parentBone.Children.Add(internalBone);
                            }
                            else
                            {
                                // If the parent bone is not found, it means we haven't processed the parent node yet. This can happen if the bones are not in a strict parent-child order in the assimp scene graph. In this case, we can just add the bone to the dictionary and link it later when we find its parent.
                                // This is a bit of a hack, but it should work as long as we eventually find all the bones and their parents.
                                boneDictionary[child.Name] = internalBone;
                            }
                        }
                        TraverseBoneNodes(child); // Recursively traverse child nodes to find more bones
                    }
                }
            }
            TraverseBoneNodes(rootNode);
            // All bones should now be in the bone hierachy

            // Now we need to merge the meshes for each node, and assign materials to the faces
            // Create them, then put them in the internal scene model's mesh list, which will be used for exporting to source engine formats
            // internalScene.Meshes

            void MergeMeshesAtNode(Assimp.Node node)
            {
                // Get all meshes associated with this node
                var meshes = node.MeshIndices.Select(index => assimpScene.Meshes[index]).ToList();
                if (meshes.Count > 0)
                {
                    MeshData internalMesh = new()
                    {
                        Name = node.Name, // Use the node name for the mesh name, as this is usually the original object name in the source model
                    };
                    for (int i = 0; i < meshes.Count; i++)
                    {
                        var mesh = meshes[i];
                        // Append vertices, normals, uvs, and faces to the internal mesh

                        // TODO: See if node transformation required.
                        // Probably not though, since we applied PreTransformVertifes
                        // Don't do it for now

                        int baseVertex = internalMesh.Vertices.Count; // Keep track of the base vertex index for this mesh, so we can correctly offset the face indices when merging

                        foreach (var vertex in mesh.Vertices)
                        {
                            internalMesh.Vertices.Add(vertex.ToVector3());
                        }

                        foreach (var normal in mesh.Normals)
                        {
                            internalMesh.Normals.Add(normal.ToVector3());
                        }

                        foreach (var tangent in mesh.Tangents)
                        {
                            internalMesh.Tangents.Add(tangent.ToVector3());
                        }
                        foreach (var bitangent in mesh.BiTangents)
                        {
                            internalMesh.BiTangents.Add(bitangent.ToVector3());
                        }

                        foreach (var uv in mesh.TextureCoordinateChannels[0]) // Assuming only one UV channel for now
                        {
                            internalMesh.UVs.Add(uv.ToVector3().xy);
                        }
                        foreach (var face in mesh.Faces)
                        {
                            if (face.IndexCount != 3)
                            {
                                throw new NotSupportedException($"Only triangular faces are supported. Found a face with {face.IndexCount} indices.");
                            }
                            internalMesh.Faces.Add(Tuple.Create(face.Indices[0], face.Indices[1], face.Indices[2]));
                            internalMesh.MaterialsByFace.Add(assimpScene.Materials[mesh.MaterialIndex].Name); // Assign material name based on the mesh's material index
                        }

                        // Bone weights are a bit more complicated, as they require us to map the bone names to the internal bone hierarchy, and then assign the weights to the vertices. We also need to handle the case where a vertex is influenced by multiple bones, which is common in skinned meshes.
                        // PAIN AND SUFFERING

                        for (int j = 0; j < mesh.Vertices.Count; j++)
                        {
                            internalMesh.BoneWeights.Add(new List<Tuple<string, float>>());
                        }
                        foreach (var bone in mesh.Bones)
                        {
                            string boneName = bone.Name;
                            foreach (var weight in bone.VertexWeights)
                            {
                                // Does this work?
                                // Will the vertex index match between original hierarchy, and the internal one?
                                // It should, since we are merging meshes at the node level, and the vertex indices are local to the mesh. As long as we append the vertices in the same order as they appear in the mesh, the indices should match up correctly.
                                // TODO: Test this thoroughly, as bone weights are really important for skinned meshes, and any mismatch in vertex indices could lead to completely broken animations.
                                int vertexId = weight.VertexID;
                                float vertexWeight = weight.Weight;
                                internalMesh.BoneWeights[vertexId].Add(Tuple.Create(boneName, vertexWeight));
                            }
                        }


                        // TODO: Blend shapes

                        // End of mesh merging for this node
                    }

                    // After merging all meshes for this node, we should have a single mesh that represents the entire object part,
                    // with correctly assigned materials and bone weights.
                    // We can then add this merged mesh to the internal scene model's mesh list, which will be used for exporting to source engine formats.

                    // Add the merged mesh to the internal scene model's mesh list
                    internalScene.Meshes.Add(internalMesh);
                }

                // Recursively merge meshes for child nodes
                foreach (var child in node.Children)
                {
                    MergeMeshesAtNode(child);
                }
            }

            // Start merging meshes from the root node
            MergeMeshesAtNode(rootNode);

            // At this point, we have the fully populated internal scene model

            return internalScene;
        }

        private Assimp.Scene ImportAssimp(string filePath)
        {

            // Setup the importer context
            AssimpContext context = new AssimpContext();

            // Exclude cameras and lights from the imported scene, as they are not relevant for our purposes
            context.SetConfig(new Assimp.Configs.RemoveComponentConfig(
                Assimp.ExcludeComponent.Cameras |       // Cameras are unused
                Assimp.ExcludeComponent.Lights |        // Lights are unused
                Assimp.ExcludeComponent.Animations |    // Animations are outside the scope of this project
                Assimp.ExcludeComponent.Colors          // Colors unusable in source
                ));

            // Actually import the model
            Assimp.Scene assimpScene = context.ImportFile(filePath,
                    PostProcessSteps.GenerateNormals |          // Generate normals if they are missing
                    PostProcessSteps.JoinIdenticalVertices |    // Remove duplicate vertices. I hate modellers for this.
                    PostProcessSteps.Triangulate |              // Triangulaate all faces.
                    PostProcessSteps.PreTransformVertices |     // Apply all transformations to vertices. If not baked, makes it horrible to debug.
                    PostProcessSteps.FlipUVs |                  // Flip UV coordinates. Needed for correct texture mapping.
                    PostProcessSteps.CalculateTangentSpace      // Calculate tangent and bitangent vectors. Makes normal mapping and phong work properly.
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
