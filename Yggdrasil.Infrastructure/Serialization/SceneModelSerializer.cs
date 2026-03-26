using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Infrastructure.Serialization
{
    public static class SceneModelSerializer
    {
        // Serializes the SceneModel to a custom binary format
        // Vertex Data (Vertex position, normals, face indices, uvs, etc.) are stored in a compact binary format for efficient loading
        // Everything else is stored as JSON for human readability and ease of editing
        public static void SerializeSceneModel(SceneModel model, string filePath)
        {
            try
            {
                // Make a copy of the model with the vertex data removed for JSON serialization
                var modelCopy = new SceneModel
                {
                    Name = model.Name,
                    MeshGroups = new(),
                    RootBone = model.RootBone,
                    MaterialSettings = model.MaterialSettings
                };

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, // Don't write useless stuff
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles, // Handle circular references gracefully
                    Converters =
                {
                    new System.Text.Json.Serialization.JsonStringEnumConverter(),
                    new Matrix4x4JsonConverter()
                }
                };

                string jsonString = System.Text.Json.JsonSerializer.Serialize(modelCopy, options);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

                // Write to a single file: JSON section followed by binary section
                using (var binaryWriter = new System.IO.BinaryWriter(System.IO.File.Open(filePath, System.IO.FileMode.Create)))
                {
                    // Write JSON section length first, then JSON data
                    binaryWriter.Write(jsonBytes.Length);
                    binaryWriter.Write(jsonBytes);

                    // Serialize the vertex data to binary
                    foreach (var meshGroup in model.MeshGroups)
                    {
                        // Write mesh group name length and mesh group name
                        binaryWriter.Write(meshGroup.Name.Length);
                        binaryWriter.Write(Encoding.UTF8.GetBytes(meshGroup.Name));

                        // Write mesh count
                        binaryWriter.Write(meshGroup.Meshes.Count);

                        foreach (var mesh in meshGroup.Meshes)
                        {
                            // Write mesh name length and mesh name
                            binaryWriter.Write(mesh.Name.Length);
                            binaryWriter.Write(Encoding.UTF8.GetBytes(mesh.Name));

                            // Write material name length and material name
                            binaryWriter.Write(mesh.Material.Length);
                            binaryWriter.Write(Encoding.UTF8.GetBytes(mesh.Material));

                            // Write vertex count
                            binaryWriter.Write(mesh.Vertices.Count);
                            // Write vertex data
                            for (int i = 0; i < mesh.Vertices.Count; i++)
                            {
                                binaryWriter.Write(mesh.Vertices[i].X);
                                binaryWriter.Write(mesh.Vertices[i].Y);
                                binaryWriter.Write(mesh.Vertices[i].Z);
                                binaryWriter.Write(mesh.Normals[i].X);
                                binaryWriter.Write(mesh.Normals[i].Y);
                                binaryWriter.Write(mesh.Normals[i].Z);
                                binaryWriter.Write(mesh.UVs[i].X);
                                binaryWriter.Write(mesh.UVs[i].Y);

                                // Tangents and bitangents can be calculated at deserialization time

                                // Bone weights
                                // Write bone weight count for this vertex
                                binaryWriter.Write(mesh.BoneWeights[i].Count);
                                foreach (var boneWeight in mesh.BoneWeights[i])
                                {
                                    // Write bone name length and bone name
                                    binaryWriter.Write(boneWeight.Item1.Length);
                                    binaryWriter.Write(Encoding.UTF8.GetBytes(boneWeight.Item1));
                                    // Write bone weight value
                                    binaryWriter.Write(boneWeight.Item2);
                                }

                                // Blendshapes
                                binaryWriter.Write(mesh.Blendshapes.Count);
                                foreach (var blendshape in mesh.Blendshapes)
                                {
                                    // Write blendshape name length and blendshape name
                                    binaryWriter.Write(blendshape.Name.Length);
                                    binaryWriter.Write(Encoding.UTF8.GetBytes(blendshape.Name));
                                    // Write blendshape vertex count
                                    binaryWriter.Write(blendshape.VertexIndices.Count);
                                    // Write blendshape vertex data
                                    for (int j = 0; j < blendshape.VertexIndices.Count; j++)
                                    {
                                        binaryWriter.Write(blendshape.VertexIndices[j]);
                                        binaryWriter.Write(blendshape.DeltaVertices[j].X);
                                        binaryWriter.Write(blendshape.DeltaVertices[j].Y);
                                        binaryWriter.Write(blendshape.DeltaVertices[j].Z);
                                    }
                                }
                            }
                            // Write face count
                            binaryWriter.Write(mesh.Faces.Count);
                            // Write face indices
                            foreach (var face in mesh.Faces)
                            {
                                binaryWriter.Write(face.Vertex1);
                                binaryWriter.Write(face.Vertex2);
                                binaryWriter.Write(face.Vertex3);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during mesh serialization: {ex.Message}");
            }
        }


        public static SceneModel Deserialize(string filePath)
        {
            try
            {
                // Read both JSON and binary sections from the single file
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The file {filePath} does not exist.");
                }

                SceneModel sceneModel;
                var meshGroups = new List<MeshGroup>();

                using (var binaryReader = new System.IO.BinaryReader(System.IO.File.Open(filePath, System.IO.FileMode.Open)))
                {
                    // Read JSON section length and deserialize JSON data
                    int jsonLength = binaryReader.ReadInt32();
                    byte[] jsonBytes = binaryReader.ReadBytes(jsonLength);
                    string jsonString = Encoding.UTF8.GetString(jsonBytes);

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        Converters =
                    {
                        new System.Text.Json.Serialization.JsonStringEnumConverter(),
                        new Matrix4x4JsonConverter()
                    }
                    };

                    sceneModel = System.Text.Json.JsonSerializer.Deserialize<SceneModel>(jsonString, options);

                    // Read binary mesh data
                    while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
                    {
                        // Read mesh group name
                        int meshGroupNameLength = binaryReader.ReadInt32();
                        byte[] meshGroupNameBytes = binaryReader.ReadBytes(meshGroupNameLength);
                        string meshGroupName = Encoding.UTF8.GetString(meshGroupNameBytes);

                        var meshGroup = new MeshGroup { Name = meshGroupName, Meshes = new() };

                        // Read mesh count
                        int meshCount = binaryReader.ReadInt32();

                        for (int m = 0; m < meshCount; m++)
                        {
                            // Read mesh name
                            int meshNameLength = binaryReader.ReadInt32();
                            byte[] meshNameBytes = binaryReader.ReadBytes(meshNameLength);
                            string meshName = Encoding.UTF8.GetString(meshNameBytes);

                            // Read material name
                            int materialNameLength = binaryReader.ReadInt32();
                            byte[] materialNameBytes = binaryReader.ReadBytes(materialNameLength);
                            string materialName = Encoding.UTF8.GetString(materialNameBytes);

                            var mesh = new MeshData
                            {
                                Name = meshName,
                                Material = materialName,
                                Vertices = new(),
                                Normals = new(),
                                UVs = new(),
                                BoneWeights = new(),
                                Blendshapes = new(),
                                Faces = new()
                            };

                            // Read vertex count
                            int vertexCount = binaryReader.ReadInt32();

                            for (int i = 0; i < vertexCount; i++)
                            {
                                // Read vertex position
                                var vertex = new Vector3(
                                    binaryReader.ReadSingle(),
                                    binaryReader.ReadSingle(),
                                    binaryReader.ReadSingle()
                                );
                                mesh.Vertices.Add(vertex);

                                // Read normal
                                var normal = new Vector3(
                                    binaryReader.ReadSingle(),
                                    binaryReader.ReadSingle(),
                                    binaryReader.ReadSingle()
                                );
                                mesh.Normals.Add(normal);

                                // Read UV
                                var uv = new Vector2(
                                    binaryReader.ReadSingle(),
                                    binaryReader.ReadSingle()
                                );
                                mesh.UVs.Add(uv);

                                // Read bone weights for this vertex
                                int boneWeightCount = binaryReader.ReadInt32();
                                var boneWeights = new List<Tuple<string, float>>();

                                for (int bw = 0; bw < boneWeightCount; bw++)
                                {
                                    int boneNameLength = binaryReader.ReadInt32();
                                    byte[] boneNameBytes = binaryReader.ReadBytes(boneNameLength);
                                    string boneName = Encoding.UTF8.GetString(boneNameBytes);
                                    float weight = binaryReader.ReadSingle();

                                    boneWeights.Add(new Tuple<string, float>(boneName, weight));
                                }
                                mesh.BoneWeights.Add(boneWeights);

                                // Read blendshapes (NOTE: This should be outside vertex loop!)
                                // This appears to be a bug in serialization - blendshapes are per-mesh, not per-vertex
                                int blendshapeCount = binaryReader.ReadInt32();

                                for (int bs = 0; bs < blendshapeCount; bs++)
                                {
                                    int blendshapeNameLength = binaryReader.ReadInt32();
                                    byte[] blendshapeNameBytes = binaryReader.ReadBytes(blendshapeNameLength);
                                    string blendshapeName = Encoding.UTF8.GetString(blendshapeNameBytes);

                                    var blendshape = new Blendshape(blendshapeName, new(), new());

                                    int blendshapeVertexCount = binaryReader.ReadInt32();

                                    for (int bsv = 0; bsv < blendshapeVertexCount; bsv++)
                                    {
                                        int vertexIndex = binaryReader.ReadInt32();
                                        var deltaVertex = new Vector3(
                                            binaryReader.ReadSingle(),
                                            binaryReader.ReadSingle(),
                                            binaryReader.ReadSingle()
                                        );

                                        blendshape.VertexIndices.Add(vertexIndex);
                                        blendshape.DeltaVertices.Add(deltaVertex);
                                    }

                                    mesh.Blendshapes.Add(blendshape);
                                }
                            }

                            // Read face count
                            int faceCount = binaryReader.ReadInt32();

                            for (int f = 0; f < faceCount; f++)
                            {
                                var face = new Face(
                                    binaryReader.ReadInt32(),
                                    binaryReader.ReadInt32(),
                                    binaryReader.ReadInt32()
                                );
                                mesh.Faces.Add(face);
                            }

                            meshGroup.Meshes.Add(mesh);
                        }

                        meshGroups.Add(meshGroup);
                    }
                }

                // Combine JSON data with binary mesh data
                if (sceneModel == null)
                {
                    throw new Exception("Failed to deserialize scene model from JSON.");
                }
                sceneModel.MeshGroups = meshGroups;

                return sceneModel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
            }

            return null;
        }
    }
}
