using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Cli.Composition;
using Yggdrassil.Cli.Parsing;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Cli.Commands.ProjectEditing
{
    internal class ProjectCommands
    {

        // import <model-file>
        public static void Import(string[] args, Project project, Services services)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: import <model-file>");
                return;
            }

            string modelFile = args[1];
            var scene = services.Importer.ImportModelAsync(modelFile).Result;
            project.Scene = scene;
        }

        public static void Rename(string[] args, Project project)
        {
            project.Name = string.Join(" ", args.Skip(1));
        }

        public static void Save(Project project)
        {
            project.Save();
        }

        public static void Output(string[] args, Project project)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: output <output-directory>");
                return;
            }
            string outputDirectory = args[1];
            project.Build.OutputDirectory = outputDirectory;
        }

        // export [--out <output-directory>] [--format <smd/dmx>] // Exports the project to the project directory, or to the specified output directory if provided. Optionally specify the export format (default is smd).
        public static void Export(string[] args, Project project, Services services)
        {
            if (!string.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("Usage: export [--out <output-directory>] [--format <smd/dmx>]");
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
                if (ArgReader.ParseArgument(args, "--format", out string? format))
                {
                    if (!string.IsNullOrEmpty(format))
                    {
                        if (format == "smd")
                        {
                            if (services.SmdExporter != null)
                            {
                                services.SmdExporter.ExportSceneAsync(outputDirectory, project.Scene);
                            }
                            else
                            {
                                services.GeneralExporter.ExportSceneAsync(outputDirectory, project.Scene);
                            }
                        }
                        else if (format == "dmx")
                        {
                            if (services.DmxExporter != null)
                            {
                                services.DmxExporter.ExportSceneAsync(outputDirectory, project.Scene);
                            }
                            else
                            {
                                Console.WriteLine("DMX export is not supported. Falling back to general exporter.");
                                services.GeneralExporter.ExportSceneAsync(outputDirectory, project.Scene);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Unsupported format: {format}. Supported formats are: smd, dmx. Falling back to general exporter.");
                            services.GeneralExporter.ExportSceneAsync(outputDirectory, project.Scene);
                        }

                        return;
                    }
                }

                services.GeneralExporter.ExportSceneAsync(outputDirectory, project.Scene);
            }

        }

        public static void Bind(string[] args, Project project)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: bind <bone-name> <bone-slot>");
                return;
            }
            string sourceBone = args[1];
            string targetBone = args[2];

            project.RigMapping.GetRigSlotFromName(targetBone).AssignedBone = sourceBone;
        }

        public static void Unbind(string[] args, Project project)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: unbind <bone-slot>");
                return;
            }
            string targetBone = args[1];
            project.RigMapping.GetRigSlotFromName(targetBone).AssignedBone = null;
        }

        public static void List(string[] args, Project project)
        {
            var type = args.Length > 1 ? args[1].ToLower() : "summary";

            if (type == "summary")
            {
                // Print a summary of the project, not including the below options

                Console.WriteLine($"Project Name: {project.Name}");
                Console.WriteLine($"Project Directory: {project.Directory}");
                Console.WriteLine($"Target Animations: {project.Qc.AnimationProfile}");
                Console.WriteLine($"Output Directory: {project.Build.OutputDirectory}");
                Console.WriteLine($"Surface Prop: {project.Qc.SurfaceProp}");

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
                void printBone(Yggdrassil.Domain.Scene.Bone bone, int indent = 0)
                {
                    if (bone != null)
                    {
                        Console.WriteLine($"{new string(' ', indent * 2)}- {bone.Name}");
                        foreach (var child in bone.Children)
                        {
                            var childBone = child as Yggdrassil.Domain.Scene.Bone;
                            if (childBone != null)
                                printBone(childBone, indent + 1);
                        }
                    }
                }
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
                var enumerator = project.RigMapping.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var slot = enumerator.Current;
                    Console.WriteLine($"Slot: {slot.DisplayName} Valve Bone: {slot.AssignedBone} | Assigned Bone: {(slot.AssignedBone != null ? slot.AssignedBone : "")}");
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
        }


        public static void AnimationProfile(string[] args, Project project)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: animprofile <animation-profile>");
                return;
            }
            string animProfile = args[1];

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
                    break;
                case "p_female":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.FemalePlayer;
                    break;
                case "npc_combine":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.CombineNPC;
                    break;
                case "npc_metrocop":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.MetrocopNPC;
                    break;
                case "npc_male":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.MaleNPC;
                    break;
                case "npc_female":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.FemaleNPC;
                    break;
                case "ragdoll":
                    project.Qc.AnimationProfile = Domain.QC.AnimationProfile.RagdollOnly;
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
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: materialpath <add/remove> <relative-path>");
                return;
            }
            string action = args[1].ToLower();
            string path = args[2];
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
