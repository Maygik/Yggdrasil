using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Scene;
using Vector2 = Yggdrassil.Types.Vector2;
using Vector3 = Yggdrassil.Types.Vector3;

namespace Yggdrassil.Infrastructure.Import
{
    public static class SmdImporter
    {
        public static SceneModel ImportSmd(string fileText, string name)
        {
            // Make sure it's an SMD
            // (Simple check, just see if it starts with "version 1")
            if (!fileText.StartsWith("version 1"))
            {
                throw new ArgumentException("File is not a valid SMD file.");
            }

            var lines = fileText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Build the nodes hierarchy
            var nodes = new Dictionary<int, Bone>();
            int lineIndex = 0;

            while (lineIndex < lines.Length && !lines[lineIndex].StartsWith("nodes"))
            {
                lineIndex++;
            }
            if (lineIndex >= lines.Length)
            {
                throw new ArgumentException("SMD file is missing 'nodes' section.");
            }
            Console.WriteLine("Parsing nodes...");
            while (lineIndex < lines.Length && !lines[lineIndex].StartsWith("end"))
            {
                var line = lines[lineIndex].Trim();
                if (line.StartsWith("nodes") || line.StartsWith("end"))
                {
                    lineIndex++;
                    continue;
                }
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    int id = int.Parse(parts[0]);
                    string nodeName = parts[1].Trim('"');
                    int parentId = int.Parse(parts[2]);
                    var bone = new Bone
                    {
                        Name = nodeName,
                        IsDeform = true // Assume all bones are deform bones for now.
                    };
                    nodes[id] = bone;
                }
                lineIndex++;
            }
            Console.WriteLine($"Parsed {nodes.Count} nodes.");



            // Build the hierarchy
            Console.WriteLine("Building bone hierarchy...");
            Bone? rootBone = null;
            foreach (var kvp in nodes)
            {
                int id = kvp.Key;
                Bone bone = kvp.Value;
                int parentId = -1; // Default to -1 for root
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith($"{id} "))
                    {
                        var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            parentId = int.Parse(parts[2]);
                            break;
                        }
                    }
                }
                if (parentId == -1)
                {
                    rootBone = bone; // This is the root bone
                }
                else if (nodes.ContainsKey(parentId))
                {
                    nodes[parentId].AddChild(bone);
                }
            }
            Console.WriteLine("Bone hierarchy built successfully.");

            // Parse the skeleton pose
            Dictionary<int, (Vector3 position, Vector3 rotation)> bonePoses = new Dictionary<int, (Vector3, Vector3)>();
            while (lineIndex < lines.Length && !lines[lineIndex].StartsWith("skeleton"))
            {
                lineIndex++;
            }
            if (lineIndex >= lines.Length)
            {
                throw new ArgumentException("SMD file is missing 'skeleton' section.");
            }

            Console.WriteLine("Parsing skeleton pose...");
            while (lineIndex < lines.Length && !lines[lineIndex].StartsWith("end"))
            {
                var line = lines[lineIndex].Trim();
                if (line.StartsWith("skeleton") || line.StartsWith("end"))
                {
                    lineIndex++;
                    continue;
                }
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 7)
                {
                    int boneId = int.Parse(parts[0]);
                    float posX = float.Parse(parts[1]);
                    float posY = float.Parse(parts[2]);
                    float posZ = float.Parse(parts[3]);
                    float rotX = float.Parse(parts[4]);
                    float rotY = float.Parse(parts[5]);
                    float rotZ = float.Parse(parts[6]);
                    bonePoses[boneId] = (new Vector3 { X = posX, Y = posY, Z = posZ }, new Vector3 { X = rotX, Y = rotY, Z = rotZ });
                }
                lineIndex++;
            }
            Console.WriteLine($"Parsed poses for {bonePoses.Count} bones.");
            Console.WriteLine("Assigning poses to bones...");
            foreach (var kvp in bonePoses)
            {
                int boneId = kvp.Key;
                if (nodes.ContainsKey(boneId))
                {
                    var bone = nodes[boneId];
                    var pose = kvp.Value;
                    bone.LocalPosition = pose.position;
                    bone.LocalRotation = Quaternion.FromEulerAngles(pose.rotation);
                }
                else
                {
                    Console.WriteLine($"Warning: Bone ID {boneId} in skeleton pose does not exist in nodes.");
                }
            }
            Console.WriteLine("Poses assigned to bones successfully.");

            // For now, we will ignore the "triangles" section and just create an empty model with the skeleton.

            var model = new SceneModel
            {
                Name = name,
                RootBone = rootBone
            };

            Console.WriteLine("SMD import completed successfully.");

            Console.WriteLine($"Model Name: {model.Name}");

            return model;
        }

    }
}
