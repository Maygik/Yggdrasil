using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Infrastructure.Import
{
    public static class SmdImporter
    {
        public static SceneModel ImportSmd(string filePath)
        {
            // Check that we're importing an SMD file
            if (Path.GetExtension(filePath).ToLower() != ".smd")
            {
                throw new ArgumentException($"The file '{filePath}' is not an SMD file.");
            }

            // Check that the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' was not found.");
            }

            // Step 1: Read the file contents
            var fileContents = File.ReadAllLines(filePath);

            SceneModel sceneModel = new SceneModel();
            sceneModel.Name = Path.GetFileNameWithoutExtension(filePath);

            // Step 2: Parse the file contents
            
            // First line should be "version x"
            if (!fileContents[0].StartsWith("version "))
            {
                throw new FormatException("The SMD file is missing the version header.");
            }

            // After that, we have the nodes section, which starts with "nodes" and ends with "end"
            int nodesStartIndex = Array.IndexOf(fileContents, "nodes");
            if (nodesStartIndex == -1)
            {
                throw new FormatException("The SMD file is missing the nodes section.");
            }

            // Parse the nodes, building the bone hierarchy
            Dictionary<string, Bone> bonesById = new Dictionary<string, Bone>();
            int endOfNodesIndex = -1;
            int currentIndex = nodesStartIndex + 1;
            foreach (var line in fileContents.Skip(nodesStartIndex + 1))
            {
                if (line == "end")
                {
                    endOfNodesIndex = currentIndex;
                    break;
                }
                // Each line in the nodes section should be in the format: id "name" parentId
                var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // first part is the id, last part is the parent id, and everything in between is the bone name
                // They're ints, but we 'll store them as strings for now
                string id = parts[0];
                string parentId = parts.Last();

                string boneName = string.Join(" ", parts.Skip(1).Take(parts.Length - 2)).Trim('"');
                Bone bone = new Bone(boneName);
                bonesById[id] = bone;
                if (parentId != "-1")
                {
                    if (!bonesById.ContainsKey(parentId))
                    {
                        throw new FormatException($"The SMD file references a parent bone with id '{parentId}' that does not exist.");
                    }
                    Bone parentBone = bonesById[parentId];
                    parentBone.Children.Add(bone);
                }
                else
                {
                    // This is the root bone
                    sceneModel.RootBone = bone;
                }

                currentIndex++;
            }

            // Now we can parse the "skeleton" section, it technically contains keyframes BUT we don't care about that for now, we just want the bone transforms for frame 0
            // section starts with "skeleton" and ends with "end"
            // We want to read from "time 0" to the next time or the end of the section, whichever comes first
            while (currentIndex < fileContents.Length && fileContents[currentIndex] != "skeleton")
            {
                currentIndex++;
            }
            if (currentIndex == fileContents.Length)
            {
                throw new FormatException("The SMD file is missing the skeleton section.");
            }

            // We are at the start of skeleton
            // Move to the next line, which should be "time 0"
            currentIndex++;
            if (currentIndex == fileContents.Length || !fileContents[currentIndex].StartsWith("time "))
            {
                throw new FormatException("The SMD file is missing the time 0 keyframe in the skeleton section.");
            }
            // We are at the time 0 keyframe, now we want to read until the next time or the end of the section
            while (currentIndex < fileContents.Length && !fileContents[currentIndex].StartsWith("time ") && fileContents[currentIndex] != "end")
            {
                // Skeleton line layout:
                // boneId posX posY posZ rotX rotY rotZ
                // All are in local space, and the rotations are in radians
                var parts = fileContents[currentIndex].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string boneId = parts[0];

                if (!bonesById.ContainsKey(boneId))
                {
                    throw new FormatException($"The SMD file references a bone with id '{boneId}' in the skeleton section that does not exist in the nodes section.");
                }

                float posX = float.Parse(parts[1]);
                float posY = float.Parse(parts[2]);
                float posZ = float.Parse(parts[3]);
                float rotX = float.Parse(parts[4]);
                float rotY = float.Parse(parts[5]);
                float rotZ = float.Parse(parts[6]);

                // Set the bone's local transform based on the position and rotation
                Bone bone = bonesById[boneId];
                bone.LocalPosition = new Vector3<float>(posX, posY, posZ);
                bone.LocalRotation = Quaternion.FromEulerAngles(rotX, rotY, rotZ);

                currentIndex++;
            }


            // Now we can parse the triangles section
            while (currentIndex < fileContents.Length && fileContents[currentIndex] != "triangles")
            {
                currentIndex++;
            }
            // Do not need to ensure that the triangles section exists, as some SMD files may not have it, and we can still import the bones without it

            if (currentIndex < fileContents.Length && fileContents[currentIndex] == "triangles")
            {
                // Parse the triangles section, which contains the mesh data
                // Each triangle is defined by 4 lines: the first line is the material name, and the next three lines are the vertices of the triangle
                var currentMesh = new MeshGroup();
                // Loop until end
                while (currentIndex < fileContents.Length && fileContents[currentIndex] != "end")
                {
                    // First line is the material name
                    string materialName = fileContents[currentIndex].Trim();
                    currentMesh.Name = materialName;
                    currentIndex++;
                    if (!currentMesh.Meshes.Any(m => m.Material == materialName))
                    {
                        // Create a new mesh for this material if it doesn't already exist
                        MeshData meshData = new MeshData();
                        meshData.Material = materialName;
                        currentMesh.Meshes.Add(meshData);
                    }
                    // Next three lines are the vertices of the triangle
                    for (int i = 0; i < 3; i++)
                    {
                        if (currentIndex >= fileContents.Length)
                        {
                            throw new FormatException("The SMD file ended unexpectedly while parsing the triangles section.");
                        }
                        var vertexParts = fileContents[currentIndex].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // Vertex line layout:
                        // posX posY posZ normalX normalY normalZ uvX uvY numWeights [boneId weight]...
                        float posX = float.Parse(vertexParts[0]);
                        float posY = float.Parse(vertexParts[1]);
                        float posZ = float.Parse(vertexParts[2]);
                        float normalX = float.Parse(vertexParts[3]);
                        float normalY = float.Parse(vertexParts[4]);
                        float normalZ = float.Parse(vertexParts[5]);
                        float uvX = float.Parse(vertexParts[6]);
                        float uvY = float.Parse(vertexParts[7]);
                        int numWeights = int.Parse(vertexParts[8]);
                        vertex.Position = new Vector3<float>(posX, posY, posZ);
                        vertex.Normal = new Vector3<float>(normalX, normalY, normalZ);
                        vertex.UV = new Vector2<float>(uvX, uvY);
                        for (int j = 0; j < numWeights; j++)
                        {
                            string boneId = vertexParts[9 + j * 2];
                            float weight = float.Parse(vertexParts[10 + j * 2]);
                            if (!bonesById.ContainsKey(boneId))
                            {
                                throw new FormatException($"The SMD file references a bone with id '{boneId}' in the triangles section that does not exist in the nodes section.");
                            }
                            Bone bone = bonesById[boneId];
                            vertex.Weights.Add((bone, weight));
                        }
                        currentMesh.Vertices.Add(vertex);
                        currentIndex++;
                    }
                    sceneModel.MeshGroups.Add(currentMesh);
                }
            }



        }
    }
}
