using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Cli.Parsing;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.QC;
using Yggdrassil.Domain.Scene;
using Yggdrassil.Infrastructure.Export;

namespace Yggdrassil.Cli.Commands.ProjectEditing
{
    internal class ProjectCommands
    {

        // import <model-file>
        public static void Import(string[] args, Project project, AppServices services)
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

            bool shouldAutoMap = ArgReader.HasFlag(args, "--automap");

            var request = new ImportModelRequest(modelFile, project, shouldAutoMap);
            var result = services.ImportModel.Execute(request);
            EditingProjectCommand.PrintServiceResult(result);
        }

        public static void Rename(string[] args, Project project, AppServices services)
        {
            var newName = string.Join(" ", args);

            var result = services.ProjectEditor.Rename(project, newName);
            EditingProjectCommand.PrintServiceResult(result);
        }

        public static void Save(Project project, AppServices services)
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

            var request = new SaveProjectRequest
            {
                project = project
            };

            var result = services.SaveProject.Execute(request);
            EditingProjectCommand.PrintServiceResult(result);

        }

        public static void Output(string[] args, Project project, AppServices services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: output <output-directory>");
                return;
            }

            string outputDirectory = string.Join(" ", args);

            var result = services.ProjectEditor.SetOutputDirectory(project, outputDirectory);
            EditingProjectCommand.PrintServiceResult(result);
        }

        // export <all/qc/mesh> [--out <output-directory>] [--format <smd/dmx>] // Exports the project to the project directory, or to the specified output directory if provided. Optionally specify the export format (default is smd).
        public static void Export(string[] args, Project project, AppServices services)
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



            ArgReader.ParseArgument(args, "--out", out string? outputOverride);

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

            var request = new ExportBuildRequest
            {
                Project = project,
                ExporterOverride = exporter,
                exportMeshes = args[0] == "mesh" || args[0] == "all",
                exportQc = args[0] == "qc" || args[0] == "all",
                outputDirectoryOverride = outputOverride
            };

            var result = services.ExportBuild.Execute(request);
            EditingProjectCommand.PrintServiceResult(result);

            if (result.Success)
            {
                Console.WriteLine($"Export successful! Files written to {outputOverride ?? project.Build.OutputDirectory}");
            }
            else
            {
                Console.WriteLine($"Export failed: {result.ErrorMessage}");
            }
        }

        public static void Scale(string[] args, Project project, AppServices services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: scale <scale-factor>");
            }
            float scaleFactor;
            string scaleArg = args[0];
            if (float.TryParse(scaleArg, out scaleFactor))
            {
                var result = services.ProjectEditor.Scale(project, scaleFactor);
                EditingProjectCommand.PrintServiceResult(result);
            }
            else
            {
                Console.Error.WriteLine($"Could not convert {scaleArg} to a float");
            }
        }

        public static void ModelPath(string[] args, Project project, AppServices services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: modelpath <model-path>");
                return;
            }
            string modelPath = string.Join(" ", args);

            var result = services.ProjectEditor.SetModelPath(project, modelPath);
            EditingProjectCommand.PrintServiceResult(result);
        }

        // Bind a bone to a slot
        public static void Bind(string[] args, Project project, AppServices services)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: bind <bone-name> <bone-slot>");
                return;
            }
            string sourceBone = args[0];
            string targetBone = args[1];

            // if targetbone is an int, index into the rig mapping, otherwise try to find a slot with a matching logical bone or display name
            if (int.TryParse(targetBone, out int slotIndex))
            {
                var result = services.ProjectEditor.BindBone(project, sourceBone, slotIndex);
                EditingProjectCommand.PrintServiceResult(result);
                return;
            }
            else
            {
                var result = services.ProjectEditor.BindBone(project, sourceBone, targetBone);
                EditingProjectCommand.PrintServiceResult(result);
                return;
            }
        }

        // Clear the bone from a slot
        public static void Unbind(string[] args, Project project, AppServices services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: unbind <bone-slot>");
                return;
            }
            string targetBone = args[0];
            
            if (int.TryParse(targetBone, out int slotIndex))
            {
                var result = services.ProjectEditor.UnbindBone(project, slotIndex);
                EditingProjectCommand.PrintServiceResult(result);
                return;
            }
            else
            {
                var result = services.ProjectEditor.UnbindBone(project, targetBone);
                EditingProjectCommand.PrintServiceResult(result);
                return;
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


        public static void AnimationProfile(string[] args, Project project, AppServices services)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: animprofile <animation-profile>");
                return;
            }

            string animProfile = args[0];

            ServiceResult result = null!;

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
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.MalePlayer);
                    break;
                case "p_female":
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.FemalePlayer);
                    break;
                case "npc_combine":
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.CombineNPC);
                    break;
                case "npc_metrocop":
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.MetrocopNPC);
                    break;
                case "npc_male":
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.MaleNPC);
                    break;
                case "npc_female":
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.FemaleNPC);
                    break;
                case "ragdoll":
                    result = services.ProjectEditor.SetAnimationProfile(project, Domain.QC.AnimationProfile.RagdollOnly);
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

            if (result != null)
            {
                EditingProjectCommand.PrintServiceResult(result);
            }
        }


        public static void MaterialPath(string[] args, Project project, AppServices services)
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
                var result = services.ProjectEditor.AddMaterialPath(project, path);
                EditingProjectCommand.PrintServiceResult(result);
            }
            else if (action == "remove")
            {
                var result = services.ProjectEditor.RemoveMaterialPath(project, path);
                EditingProjectCommand.PrintServiceResult(result);
            }
        }

        public static void SurfaceProp(string[] args, Project project, AppServices services)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: surfaceprop <surface-prop>");
                return;
            }
            string surfaceProp = args[1];
            var result = services.ProjectEditor.SetSurfaceProp(project, surfaceProp);
            EditingProjectCommand.PrintServiceResult(result);
        }

        public static void Bodygroup(string[] args, Project project, AppServices services)
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
                var result = services.ProjectEditor.AddBodygroup(project, bodyGroupName, submodels);
                EditingProjectCommand.PrintServiceResult(result);
            }
            else if (action == "remove")
            {
                var result = services.ProjectEditor.RemoveBodygroup(project, bodyGroupName);
                EditingProjectCommand.PrintServiceResult(result);
            }
        }
    }
}
