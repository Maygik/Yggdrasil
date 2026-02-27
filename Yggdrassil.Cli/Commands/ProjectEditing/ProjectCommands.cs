using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Cli.Composition;
using Yggdrassil.Cli.Parsing;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.QC;
using Yggdrassil.Domain.Scene;
using Yggdrassil.Infrastructure.Export;
using Yggdrassil.Infrastructure.Serialization;

using Bone = Yggdrassil.Domain.Scene.Bone;

namespace Yggdrassil.Cli.Commands.ProjectEditing
{
    internal class ProjectCommands
    {

        // import <model-file>
        public static void Import(string[] args, Project project, Services services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: import <model-file> [--automap]");
                return;
            }

            string? modelFile = ArgReader.ParseFirstParameter(args);
            if (string.IsNullOrEmpty(modelFile))
            {
                Console.WriteLine("Model file path cannot be empty.");
                return;
            }

            var scene = services.Importer.ImportModelAsync(modelFile).Result;
            project.Scene = scene;

            // Auto-add every mesh group as a bodygroup with a single submodel
            foreach (var meshGroup in scene.MeshGroups)
            {
                if (!project.Qc.Bodygroups.Any(bg => bg.Name == meshGroup.Name))
                {
                    project.Qc.Bodygroups.Add(new Domain.QC.Bodygroup(meshGroup.Name, new List<string?>() { meshGroup.Name }));
                }
            }

            if (ArgReader.HasFlag(args, "--automap"))
            {
                if (scene.RootBone == null)
                {
                    Console.WriteLine($"Scene has no root bone. Skipping --automap.");
                    return;
                }

                void CheckBoneMap(Bone bone)
                {
                    var slot = project.RigMapping.TryGetRigSlotFromName(bone.Name);

                    if (slot != null)
                    {
                        slot.AssignedBone = bone.Name;
                    }

                    foreach (var child in bone.Children)
                    {
                        var childBone = child as Bone;
                        if (childBone != null)
                            CheckBoneMap(childBone);
                    }
                }
                CheckBoneMap(scene.RootBone);
            }

        }

        public static void Rename(string[] args, Project project)
        {
            project.Name = string.Join(" ", args);
        }

        public static void Save(Project project)
        {
            if (project.Directory == null)
            {
                Console.WriteLine("Project directory is not set. Cannot save.");
                return;
            }
            if (string.IsNullOrEmpty(project.Name))
            {
                Console.WriteLine("Project name cannot be empty. Cannot save.");
                return;
            }

            ProjectSerializer.SerializeProject(project, Path.Combine(project.Directory, $"{project.Name}.yggproj"));
        }

        public static void Output(string[] args, Project project)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: output <output-directory>");
                return;
            }

            string outputDirectory = string.Join(" ", args);
            project.Build.OutputDirectory = outputDirectory;
        }

        // export <all/qc/mesh> [--out <output-directory>] [--format <smd/dmx>] // Exports the project to the project directory, or to the specified output directory if provided. Optionally specify the export format (default is smd).
        public static void Export(string[] args, Project project, Services services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: export <all/qc/mesh> [--out <output-directory>] [--format <smd/dmx>]");
                return;
            }

            if (project.Scene == null)
            {
                Console.WriteLine("No scene to export. Please import a model first.");
                return;
            }
            if (project.Directory == null)
            {
                Console.WriteLine("Project directory is not set. Cannot export.");
                return;
            }

            string outputDirectory = project.Build.OutputDirectory;
            if (ArgReader.ParseArgument(args, "--out", out string? outDir))
            {
                if (!string.IsNullOrEmpty(outDir))
                    outputDirectory = outDir;
            }
            if (!string.IsNullOrEmpty(outputDirectory))
            {

                if (args[0] == "mesh" || args[0] == "all")
                {
                    // Figure out which exporter to use
                    IMeshExporter exporter = services.GeneralExporter;

                    if (ArgReader.ParseArgument(args, "--format", out string? format))
                    {
                        if (!string.IsNullOrEmpty(format))
                        {
                            if (format == "smd" && services.SmdExporter != null)
                            {
                                exporter = services.SmdExporter;
                            }
                            else if (format == "dmx" && services.DmxExporter != null)
                            {
                                exporter = services.DmxExporter;
                            }
                            else
                            {
                                Console.WriteLine($"Unsupported format: {format}. Supported formats are: smd, dmx. Falling back to general exporter.");
                            }
                        }
                    }



                    // Export the animations (Do here just to get proportions)

                    SceneModel? proportions = null;

                    if (project.Scene.RootBone != null)
                    {
                        Console.WriteLine($"Exporting animations...");

                        // Check if we need proportion trick
                        List<AnimationProfile> profilesRequiringProportionTrick = new List<AnimationProfile>()
                        {
                            Domain.QC.AnimationProfile.MalePlayer,
                            Domain.QC.AnimationProfile.FemalePlayer,
                            Domain.QC.AnimationProfile.MaleNPC,
                            Domain.QC.AnimationProfile.FemaleNPC,
                            Domain.QC.AnimationProfile.CombineNPC,
                            Domain.QC.AnimationProfile.MetrocopNPC
                        };

                        var animOutputDir = System.IO.Path.Combine(outputDirectory, "anims");
                        if (profilesRequiringProportionTrick.Contains(project.Qc.AnimationProfile))
                        {
                            Console.WriteLine($"Applying proportion trick for animation export due to selected animation profile: {project.Qc.AnimationProfile}");

                            // Create a temporary project copy with a cloned scene to avoid modifying the original
                            var tempProject = new Project
                            {
                                Scene = project.Scene.DeepClone(),
                                RigMapping = project.RigMapping,
                                Qc = project.Qc
                            };

                            ProportionTrickFactory.BuildAnimations(tempProject, out SceneModel referenceMale, out SceneModel referenceFemale, out proportions);
                            Console.WriteLine($"Exporting proportion trick animations...");

                            // Export animations to /anims
                            exporter.ExportAnimationAsync(animOutputDir, "proportions", proportions);
                            exporter.ExportAnimationAsync(animOutputDir, "reference_male", referenceMale);
                            exporter.ExportAnimationAsync(animOutputDir, "reference_female", referenceFemale);
                        }
                        else
                        {
                            // Just export a normal skeleton with the bind pose as the only frame
                            exporter.ExportAnimationAsync(animOutputDir, "ragdoll", project.Scene);
                        }

                    }
                    else
                    {
                        Console.WriteLine($"Exported meshes without skeleton to {outputDirectory}");
                    }


                    // Copy over the proportions armature
                    project.Scene.RootBone = proportions?.RootBone;


                    if (project.Scene.MeshGroups.Count == 0)
                    {
                        Console.WriteLine($"Scene has no meshes to export.");
                    }
                    else
                    {
                        exporter.ExportSceneAsync(outputDirectory, project.Scene);
                    }

                }

                if (args[0] == "qc" || args[0] == "all")
                {
                    var qc = services.Assembler.AssembleQc(project.Qc);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(outputDirectory, $"{project.Name}.qc"), qc);
                    Console.WriteLine($"QC exporter to {outputDirectory + project.Name}.qc");
                }

                

            }

        }

        public static void Scale(string[] args, Project project)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: scale <scale-factor>");
            }
            float scaleFactor;
            string scaleArg = args[0];
            if (float.TryParse(scaleArg, out scaleFactor))
            {
                if (project.Scene == null)
                {
                    Console.WriteLine("No scene to scale. Please import a model first.");
                    return;
                }

                if (project.Scene.RootBone != null)
                {
                    // Scale all bone positions

                    void ScaleBone(Bone bone)
                    {
                        bone.LocalPosition = new Vector3<float>(
                            bone.LocalPosition.X * scaleFactor,
                            bone.LocalPosition.Y * scaleFactor,
                            bone.LocalPosition.Z * scaleFactor
                        );
                        foreach (var child in bone.Children)
                        {
                            var childBone = child as Bone;
                            if (childBone != null)
                                ScaleBone(childBone);
                        }
                    }
                    ScaleBone(project.Scene.RootBone);
                }

                // No root bone, must scale all vertex positions manually
                Console.WriteLine("Scaling all vertex positions");
                foreach (var meshGroup in project.Scene.MeshGroups)
                {
                    foreach (var mesh in meshGroup.Meshes)
                    {
                        for (int i = 0; i < mesh.Vertices.Count; i++)
                        {
                            var vertex = mesh.Vertices[i];
                            vertex.X *= scaleFactor;
                            vertex.Y *= scaleFactor;
                            vertex.Z *= scaleFactor;
                            mesh.Vertices[i] = vertex;
                        }
                    }
                }

                Console.WriteLine($"Finished scaling model by a factor of {scaleFactor}");
            }
            else
            {
                Console.WriteLine($"Invalid scale factor: {scaleArg}");
            }
        }

        public static void ModelPath(string[] args, Project project)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: modelpath <model-path>");
                return;
            }
            string modelPath = string.Join(" ", args);
            project.Qc.ModelPath = modelPath;
            Console.WriteLine($"Model path set to: {modelPath}");
        }

        public static void Bind(string[] args, Project project)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: bind <bone-name> <bone-slot>");
                return;
            }
            string sourceBone = args[0];
            string targetBone = args[1];

            var slot = project.RigMapping.TryGetRigSlotFromName(targetBone);
            slot.AssignedBone = sourceBone;
            Console.WriteLine($"Bound bone \"{sourceBone}\" to slot \"{slot.DisplayName}\"");
        }

        public static void Unbind(string[] args, Project project)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: unbind <bone-slot>");
                return;
            }
            string targetBone = args[0];
            var slot = project.RigMapping.TryGetRigSlotFromName(targetBone);
            if (slot != null)
            {
                slot.AssignedBone = null;
                Console.WriteLine($"Unbound \"{targetBone}\" from the slot: \"{slot.DisplayName}\"");
            }
        }

        public static void List(string[] args, Project project)
        {
            var type = args.Length == 1 ? args[0].ToLower() : "summary";

            if (type == "summary")
            {
                // Print a summary of the project, not including the below options

                Console.WriteLine();
                Console.WriteLine($"Project Name: {project.Name}");
                Console.WriteLine($"Project Directory: {project.Directory}");
                Console.WriteLine($"Output Directory: {project.Build.OutputDirectory}");
                Console.WriteLine();

                Console.WriteLine($"Model Path: {project.Qc.ModelPath}");
                Console.WriteLine($"Target Animations: {project.Qc.AnimationProfile}");
                Console.WriteLine($"Surface Prop: {project.Qc.SurfaceProp}");
                Console.WriteLine($"Bodygroups: {project.Qc.Bodygroups.Count}");
                foreach (var bodyGroup in project.Qc.Bodygroups)
                {
                    Console.WriteLine($"\tBodygroup: {bodyGroup.Name} Submodels: {string.Join(", ", bodyGroup.Submeshes)}");
                }
                Console.WriteLine();

                // Mesh count
                Console.WriteLine($"Meshes: {project.Scene.MeshGroups.Count}");

                // Vertex/face count
                int vertexCount = 0;
                int faceCount = 0;
                foreach (var meshGroup in project.Scene.MeshGroups)
                {
                    foreach (var mesh in meshGroup.Meshes)
                    {
                        vertexCount += mesh.Vertices.Count;
                        faceCount += mesh.Faces.Count;
                    }
                }
                Console.WriteLine($"Total Vertices: {vertexCount}");
                Console.WriteLine($"Total Faces: {faceCount}");
            }
            else if (type == "bones")
            {
                void PrintBone(Yggdrassil.Domain.Scene.Bone bone, int indent = 0)
                {
                    if (bone != null)
                    {
                        Console.WriteLine($"{new string(' ', indent * 2)}- {bone.Name}");
                        foreach (var child in bone.Children)
                        {
                            var childBone = child as Yggdrassil.Domain.Scene.Bone;
                            if (childBone != null)
                                PrintBone(childBone, indent + 1);
                        }
                    }
                }
                if (project.Scene.RootBone != null)
                    PrintBone(project.Scene.RootBone);
                else
                    Console.Write("Model has no bones..");

            }
            else if (type == "materials")
            {
                foreach (var material in project.Scene.MaterialSettings.Keys)
                {
                    Console.WriteLine($"- {material}");
                }
            }
            else if (type == "meshes")
            {
                foreach (var meshGroup in project.Scene.MeshGroups)
                {
                    Console.WriteLine($"Mesh: {meshGroup.Name}");
                }
            }
            else if (type == "slots")
            {
                for (int i = 0; i < project.RigMapping.Count; i++)
                {
                    var slot = project.RigMapping[i];
                    Console.WriteLine($"{i} Slot: {slot.DisplayName} Valve Bone: {slot.LogicalBone} | Assigned Bone: {(slot.AssignedBone != null ? slot.AssignedBone : "")}");
                }
            }
            else if (type == "bodygroups")
            {
                foreach (var bodyGroup in project.Qc.Bodygroups)
                {
                    Console.WriteLine($"BodyGroup: {bodyGroup.Name}");
                    foreach (var subModel in bodyGroup.Submeshes)
                    {
                        Console.WriteLine($"\tSubModel: {subModel}");
                    }
                }
            }
            else if (type == "materialpaths")
            {
                foreach (var materialPath in project.Qc.CdMaterialsPaths)
                {
                    Console.WriteLine($"Material Path: {materialPath}");
                }
            }
            else if (type == "bounds")
            {
                if (project.Scene.MeshGroups.Count > 0)
                {
                    Yggdrassil.Domain.Scene.Vector3<float> highestVertex = new Yggdrassil.Domain.Scene.Vector3<float>(float.MinValue, float.MinValue, float.MinValue);
                    Yggdrassil.Domain.Scene.Vector3<float> lowestVertex = new Yggdrassil.Domain.Scene.Vector3<float>(float.MaxValue, float.MaxValue, float.MaxValue);
                    foreach (var meshGroup in project.Scene.MeshGroups)
                    {
                        foreach (var mesh in meshGroup.Meshes)
                        {
                            if (project.Scene.RootBone != null)
                            {
                                for (int i = 0; i < mesh.Vertices.Count; i++)
                                {
                                    var vertex = mesh.Vertices[i];
                                    var transformedVertex = new Vector3<float>(0, 0, 0);
                                    foreach (var weight in mesh.BoneWeights[i])
                                    {
                                        var bone = project.Scene.RootBone.FindBoneInChildren(weight.Item1);
                                        if (bone != null)
                                        {
                                            var matrix = bone.WorldMatrix;
                                            var weightedVertex = new Vector3<float>(
                                                vertex.X * weight.Item2,
                                                vertex.Y * weight.Item2,
                                                vertex.Z * weight.Item2
                                            );

                                            // Transform the vertex
                                            transformedVertex.X += weightedVertex.X * matrix.M00 + weightedVertex.Y * matrix.M01 + weightedVertex.Z * matrix.M02 + matrix.M03;
                                            transformedVertex.Y += weightedVertex.X * matrix.M10 + weightedVertex.Y * matrix.M11 + weightedVertex.Z * matrix.M12 + matrix.M13;
                                            transformedVertex.Z += weightedVertex.X * matrix.M20 + weightedVertex.Y * matrix.M21 + weightedVertex.Z * matrix.M22 + matrix.M23;
                                        }
                                    }

                                    // Check the position using the transformed vertex position
                                    if (transformedVertex.X > highestVertex.X) highestVertex.X = transformedVertex.X;
                                    else if (transformedVertex.X < lowestVertex.X) lowestVertex.X = transformedVertex.X;
                                    if (transformedVertex.Y > highestVertex.Y) highestVertex.Y = transformedVertex.Y;
                                    else if (transformedVertex.Y < lowestVertex.Y) lowestVertex.Y = transformedVertex.Y;
                                    if (transformedVertex.Z > highestVertex.Z) highestVertex.Z = transformedVertex.Z;
                                    else if (transformedVertex.Z < lowestVertex.Z) lowestVertex.Z = transformedVertex.Z;
                                }
                            }
                            foreach (var vertex in mesh.Vertices)
                            {
                                // Foreach vertex, check the position using the scaled vertex position through bones, if any
                                if (vertex.X > highestVertex.X) highestVertex.X = vertex.X;
                                if (vertex.Y > highestVertex.Y) highestVertex.Y = vertex.Y;
                                if (vertex.Z > highestVertex.Z) highestVertex.Z = vertex.Z;
                                if (vertex.X < lowestVertex.X) lowestVertex.X = vertex.X;
                                if (vertex.Y < lowestVertex.Y) lowestVertex.Y = vertex.Y;
                                if (vertex.Z < lowestVertex.Z) lowestVertex.Z = vertex.Z;
                            }
                        }
                    }
                    Console.WriteLine($"Model Bounds:");
                    Console.WriteLine($"\tUpper Bound: ({highestVertex.X}, {highestVertex.Y}, {highestVertex.Z})");
                    Console.WriteLine($"\tLower Bound: ({lowestVertex.X}, {lowestVertex.Y}, {lowestVertex.Z})");
                    Console.WriteLine($"\tSize: ({highestVertex.X - lowestVertex.X}, {highestVertex.Y - lowestVertex.Y}, {highestVertex.Z - lowestVertex.Z})");
                    Console.WriteLine($"\tTotal Height: {highestVertex.Z - lowestVertex.Z}");
                }
                else
                {
                    Console.WriteLine($"Model has no meshes, cannot calculate bounds.");
                }
            }
            Console.WriteLine();
        }


        public static void AnimationProfile(string[] args, Project project)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: animprofile <animation-profile>");
                return;
            }

            string animProfile = args[0];

            //p_male
            //p_female
            //npc_combine
            //npc_metrocop
            //npc_male
            //npc_female
            //ragdoll
            switch (animProfile)
            {
                case "p_male":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.MalePlayer;
                    Console.WriteLine("QC animations will now be generated for a male player");
                    break;
                case "p_female":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.FemalePlayer;
                    Console.WriteLine("QC animations will now be generated for a female player");
                    break;
                case "npc_combine":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.CombineNPC;
                    Console.WriteLine("QC animations will now be generated for a Combine NPC");
                    break;
                case "npc_metrocop":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.MetrocopNPC;
                    Console.WriteLine("QC animations will now be generated for a Metrocop NPC");
                    break;
                case "npc_male":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.MaleNPC;
                    Console.WriteLine("QC animations will now be generated for a male NPC");
                    break;
                case "npc_female":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.FemaleNPC;
                    Console.WriteLine("QC animations will now be generated for a female NPC");
                    break;
                case "ragdoll":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.RagdollOnly;
                    Console.WriteLine("QC animations will now be generated for a Ragdoll");
                    break;
                default:
                    Console.WriteLine($"Unsupported animation profile: \"{animProfile}\". Supported profiles:");
                    Console.WriteLine("\tp_male");
                    Console.WriteLine("\tp_female");
                    Console.WriteLine("\tnpc_combine");
                    Console.WriteLine("\tnpc_metrocop");
                    Console.WriteLine("\tnpc_male");
                    Console.WriteLine("\tnpc_female");
                    Console.WriteLine("\tragdoll");
                    break;
            }
        }


        public static void MaterialPath(string[] args, Project project)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: materialpath <add/remove> <relative-path>");
                return;
            }
            string action = args[0].ToLower();
            string path = args[1];
            if (action == "add")
            {
                if (!project.Qc.CdMaterialsPaths.Contains(path))
                {
                    project.Qc.CdMaterialsPaths.Add(path);
                    Console.WriteLine($"Added material path: {path}");
                }
                else
                {
                    Console.WriteLine($"Material path already exists: {path}");
                }
            }
            else if (action == "remove")
            {
                if (project.Qc.CdMaterialsPaths.Contains(path))
                {
                    project.Qc.CdMaterialsPaths.Remove(path);
                    Console.WriteLine($"Removed material path: {path}");
                }
                else
                {
                    Console.WriteLine($"Material path not found: {path}");
                }
            }
        }

        public static void SurfaceProp(string[] args, Project project)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: surfaceprop <surface-prop>");
                return;
            }
            string surfaceProp = args[1];
            project.Qc.SurfaceProp = surfaceProp;
            Console.WriteLine($"Model will not use {surfaceProp} for $surfaceprop");
        }

        public static void Bodygroup(string[] args, Project project)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: bodygroup <add/remove> <bodygroup-name> [submodel1 submodel2 ...]");
                return;
            }
            string action = args[1].ToLower();
            string bodyGroupName = args[2];
            List<string?> submodels = new List<string?>();
            var submodelArgs = args.Skip(3).ToList();
            foreach (var model in submodelArgs)
            {
                if (model.ToLower() == "blank")
                {
                    // "blank" is reserved for empty bodygroup options
                    submodels.Add(null);
                }
                else
                    submodels.Add(model);
            }

            if (action == "add")
            {
                var bodyGroup = project.Qc.Bodygroups.FirstOrDefault(bg => bg.Name == bodyGroupName);
                if (bodyGroup != null)
                {
                    bodyGroup.Submeshes.AddRange(submodels);
                }
                else
                {
                    project.Qc.Bodygroups.Add(new Domain.QC.Bodygroup(bodyGroupName, submodels));
                }
            }
            else if (action == "remove")
            {
                var bodyGroup = project.Qc.Bodygroups.FirstOrDefault(bg => bg.Name == bodyGroupName);
                if (bodyGroup != null)
                {
                    project.Qc.Bodygroups.Remove(bodyGroup);
                }
                else
                {
                    Console.WriteLine($"Bodygroup not found: {bodyGroupName}");
                }
            }
        }
    }
}
