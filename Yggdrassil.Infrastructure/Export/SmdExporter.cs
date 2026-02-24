using Assimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Scene;

using Bone = Yggdrassil.Domain.Scene.Bone;
using Quaternion = Yggdrassil.Domain.Scene.Quaternion;
using Matrix4x4 = Yggdrassil.Domain.Scene.Matrix4x4;
using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;

namespace Yggdrassil.Infrastructure.Export
{
    /// <summary>
    ///  Writes source SMD files for meshes and skeletons, which can be used in Source engine tools like StudioMDL for compiling models.
    /// </summary>
    public class SmdExporter : IMeshExporter
    {
        public SmdExporter()
        {
            // Constructor can be used for any necessary initialization, if needed.
        }

        /// <summary>
        /// Exports the given internal scene to Source SMD files
        /// Each MeshGroup in the scene will be exported as a separate SMD file
        /// Currently does not support VTF export
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="scene"></param>
        /// 
        public Task ExportSceneAsync(string folderPath, Domain.Scene.SceneModel scene) => Task.FromResult(ExportScene(folderPath, scene));
        public bool ExportScene(string folderPath, Domain.Scene.SceneModel scene)
        {
            // TODO: Convert mesh and skeleton data from the internal scene representation to the SMD format.

            var sb = new StringBuilder();

            sb.AppendLine($"version 1"); // Only supporting version 1, it's the most widely used one

            // 1: Node structure
            // The "nodes" section defines the skeleton hierarchy and assigns an ID to each bone.
            sb.Append(BuildNodes(scene, out var boneIds));

            // 2: Skeleton structure
            // The "skeleton" section defines the position and rotation of each bone. Not supporting animation, so just writes a single frame at time 0.
            sb.Append(BuildSkeleton(scene));


            string sharedStart = sb.ToString();


            // Triangle data and export is done per mesh
            foreach (var meshGroup in scene.MeshGroups)
            {
                ExportSingleMesh(folderPath, sharedStart, meshGroup, boneIds);
            }

            return true;
        }

        public void ExportSingleMesh(string folderPath, string sharedStart, Domain.Scene.MeshGroup mesh, Dictionary<string, int> boneIds)
        {
            var sb = new StringBuilder();

            // Add the shared start (nodes + skeleton)
            sb.Append(sharedStart);
            

            // 3: Triangles
            // Defines mesh geometry
            
            // Start of the block
            sb.AppendLine("triangles");

            foreach (var meshData in mesh.Meshes)
            {
                sb.Append(BuildTrianglesForMeshData(meshData, boneIds));
            }

            sb.AppendLine("end");

            // TODO: VTA export

            // Write the SMD file
            var finalSmd = sb.ToString();

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            System.IO.File.WriteAllText(folderPath + "/" + mesh.Name + ".smd", finalSmd);
            Console.WriteLine($"Exported mesh {mesh.Name} to {folderPath + mesh.Name + ".smd"}");
        }

        // Builds the "nodes" section of the SMD file, which defines the skeleton hierarchy.
        public string BuildNodes(Domain.Scene.SceneModel scene, out Dictionary<string, int> boneIds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("nodes");

            boneIds = new Dictionary<string, int>();

            // If no root, just write a single root node
            if (scene.RootBone == null)
            {
                sb.AppendLine("0 \"root\" -1");
                sb.AppendLine("end");
                boneIds["root"] = 0;
                return sb.ToString();
            }

            // Foreach bone in the skeleton, write a line with the format: <bone_id> "<bone_name>" <parent_bone_id>
            // if <parent_bone_id> is -1, it means this bone is the root bone. Will generaly be ValveBiped.Bip01_Pelvis

            int currBone = -1;

            void WriteBone(Bone bone, int parentId, Dictionary<string, int> boneIds)
            {
                currBone++;
                sb.AppendLine($"{currBone} \"{bone.Name}\" {parentId}");
                boneIds[bone.Name] = currBone;

                var thisBoneId = currBone;

                foreach (var child in bone.Children)
                {
                    if (child is Bone childBone)
                        WriteBone(childBone, thisBoneId, boneIds);
                }
            }

            WriteBone(scene.RootBone, -1, boneIds);
            sb.AppendLine("end");

            return sb.ToString();
        }

        public string BuildSkeleton(Domain.Scene.SceneModel scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("skeleton");

            sb.AppendLine("time 0"); // Not supporting animation, so just write a single frame at time 0

            // For each bone, write a line with the format: <bone_id> <pos_x> <pos_y> <pos_z> <rot_x> <rot_y> <rot_z>
            // where pos is the position of the bone relative to its parent, and rot is the rotation of the bone in Euler angles (in radians) relative to its parent.
            int currId = 0;
            void WriteBone(Bone bone)
            {
                // Get the position and rotation of the bone relative to its parent
                // First bone should use world transform though
                var pos = bone.LocalPosition;
                var rot = bone.LocalRotation.EulerAngles;

                if (currId == 0)
                {
                    pos = bone.WorldPosition;
                    rot = bone.WorldRotation.EulerAngles;
                }


                // Output positions to 6 decimal places to avoid issues with floating point precision in Source engine
                sb.AppendLine($"{currId}  {pos.X:F6} {pos.Y:F6} {pos.Z:F6}  {rot.X:F6} {rot.Y:F6} {rot.Z:F6}");
                foreach (var child in bone.Children)
                {
                    currId++;
                    if (child is Bone childBone)
                        WriteBone(childBone);
                }
            }
            if (scene.RootBone != null)
                WriteBone(scene.RootBone); // Root bone has ID 0
            else
                sb.AppendLine("0 0 0 0 0 0 0"); // If no root bone, just write a single root bone with no transformation

            sb.AppendLine("end");

            return sb.ToString();
        }



        // Creates the "triangles" section of the SMD file for a given mesh, which defines the geometry of the mesh.
        // Does not include the "triangles" or "end" lines
        public string BuildTrianglesForMeshData(Domain.Scene.MeshData mesh, Dictionary<string, int> boneIds)
        {
            var sb = new StringBuilder();

            // For each mesh in the group, write the triangles

            foreach (var triangle in mesh.Faces)
            {
                // Each triangle is stored in the format:
                // <material_name>
                // <main_bone_id> <pos_x> <pos_y> <pos_z> <normal_x> <normal_y> <normal_z> <uv_x> <uv_y> <num_bone_weights> <weight_1_bone_id> <weight_1_value> <weight_2_bone_id> <weight_2_value> ...
                // <main_bone_id> <pos_x> <pos_y> <pos_z> <normal_x> <normal_y> <normal_z> <uv_x> <uv_y> <num_bone_weights> <weight_1_bone_id> <weight_1_value> <weight_2_bone_id> <weight_2_value> ...
                // <main_bone_id> <pos_x> <pos_y> <pos_z> <normal_x> <normal_y> <normal_z> <uv_x> <uv_y> <num_bone_weights> <weight_1_bone_id> <weight_1_value> <weight_2_bone_id> <weight_2_value> ...
                // With one line per vertex of the triangle, and the first line being the material name for the triangle.


                sb.AppendLine(mesh.Material); // Material name for this triangle

                for (int vertIdx = 0; vertIdx < 3; vertIdx++)
                {
                    // Parent bone will be bone with highest weight, or 0 if no bone weights
                    // This is used as a backup if weights don't add to 1
                    // Better than the blender exporter 😎
                    string parentBone = "0";

                    string pos = $"{mesh.Vertices[triangle[vertIdx]].X:F6} {mesh.Vertices[triangle[vertIdx]].Y:F6} {mesh.Vertices[triangle[vertIdx]].Z:F6}";
                    string normal = $"{mesh.Normals[triangle[vertIdx]].X:F6} {mesh.Normals[triangle[vertIdx]].Y:F6} {mesh.Normals[triangle[vertIdx]].Z:F6}";
                    string uv = $"{mesh.UVs[triangle[vertIdx]].X:F6} {mesh.UVs[triangle[vertIdx]].Y:F6}";

                    string numLinks = mesh.BoneWeights[triangle[vertIdx]].Count.ToString();

                    List<string> boneWeights = new List<string>();
                    for (int i = 0; i < mesh.BoneWeights[triangle[vertIdx]].Count; i++)
                    {
                        if (i == 0)
                        {
                            string boneName = mesh.BoneWeights[triangle[vertIdx]][i].Item1;
                            if (boneIds.ContainsKey(boneName))
                                parentBone = boneIds[boneName].ToString();
                        }

                        var bw = mesh.BoneWeights[triangle[vertIdx]][i];
                        boneWeights.Add($"{boneIds[bw.Item1]} {bw.Item2:F6}");
                    }


                    var lineSb = new StringBuilder();
                    lineSb.Append($"{parentBone} {pos} {normal} {uv} {numLinks}");
                    foreach (var bw in boneWeights)
                    {
                        lineSb.Append($" {bw}");
                    }


                    sb.AppendLine(lineSb.ToString());
                }

            }


            return sb.ToString();
        }
    }
}