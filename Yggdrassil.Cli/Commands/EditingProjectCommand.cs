using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Cli.Composition;
using Yggdrassil.Cli.Parsing;
using Yggdrassil.Domain.Project;
using Yggdrassil.Infrastructure.Serialization;

namespace Yggdrassil.Cli.Commands
{
    public class EditingProjectCommand
    {
        public static void EditProject(Project project, Services Services)
        {
            // Write help lines for possible commands
            // import <model-file> // Imports a model file into the project
            // rename <new-project-name> // Renames the project
            // save
            // output <output-directory> // Sets the output directory for exports
            // export [--out <output-directory>] [--format <smd/dmx>] // Exports the project to the project directory, or to the specified output directory if provided. Optionally specify the export format (default is smd).
            // exit [--force] // Exits the project. If there are unsaved changes, prompts the user to save or discard them, unless --force is used, which exits immediately without saving.

            // bind <bone-name> <bone-slot> // Binds a bone to a specific slot for in-game use. Accepts slot name, or ValveBiped equivalent.
            // unbind <bone-slot> // Unbinds a bone from its slot. Accepts either the bone name or the slot name.

            // list bones           // Outputs a list of all bones in the project, along with their bound slot if any
            // list materials       // Outputs a list of all materials in the project
            // list meshes          // Outputs a list of all meshes in the project, along with any bodygroups they belong to
            // list slots           // Outputs a list of all bone slots, along with the bound bone if any
            // list bodygroups      // Outputs a list of all bodygroups, along with the meshes they contain
            // list materialpaths   // Outputs a list of all material paths the qc will use
            // list summary         // Outputs a summary of the project, including number of bones, meshes, materials, bodygroups, and any other relevant information.

            // list animation_sets // Outputs a list of all valid animation sets
            // animations <animation-set-name> // Sets the animation set to use.
            // Valid animation sets:
            // p_male
            // p_female
            // npc_combine
            // npc_metrocop
            // npc_male
            // npc_female
            // ragdoll


            // materialpath add <relative-path> // Adds a relative path to search for materials when generating the QC. Relative to the addon folder. "" means root materials folder.
            // materialpath remove <relative-path> // Removes a relative path from the list of paths to search for materials.

            // surfaceprop <surface-prop-name> // Sets the surfaceprop for the model, which determines footstep sounds and other material interactions in-game.


            // bodygroup add <group-name> <mesh-name> [extra mesh] [extra mesh] ... // Adds a bodygroup with the specified name, and adds the specified meshes to it. Optionally can specify extra meshes to add to the bodygroup.
            // bodygroup remove <group-name> // Removes the specified bodygroup

            // Enter a loop to allow the user to import a model, or change project settings using extra commands
            bool shouldExit = false;
            do
            {
                try
                {
                    Console.WriteLine("Enter a command (type 'help' for a list of commands, 'exit' to quit):");
                    var input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input))
                    {
                        continue;
                    }
                    var args = ArgReader.ExtractArguments(input);
                    var command = args[0].ToLower();
                    var commandArgs = args.Skip(1).ToArray();
                    switch (command)
                    {
                        case "help":
                            Console.WriteLine("Available commands:");
                            Console.WriteLine("Imports the specified model file into the projec");
                            Console.WriteLine("\timport <model-file>");
                            Console.WriteLine("Renames the project to the specified new name.");
                            Console.WriteLine("\trename <new-project-name>");
                            Console.WriteLine("Saves the project file with the current state of the project.");
                            Console.WriteLine("\tsave");
                            Console.WriteLine("Sets the output directory for exports.");
                            Console.WriteLine("\toutput <output-directory>");
                            Console.WriteLine("Exports the project to the project directory, or to the specified output directory if provided. Optionally specify the export format (default is smd).");
                            Console.WriteLine("\texport <all/qc/mesh> [--out <output-directory>] [--format <smd/dmx>]");
                            Console.WriteLine("Exits the project. If there are unsaved changes, prompts the user to save or discard them, unless --force is used, which exits immediately without saving.");
                            Console.WriteLine("\texit [--force]");
                            Console.WriteLine("Binds a bone to a specific slot for in-game use. Accepts slot name, or ValveBiped equivalent.");
                            Console.WriteLine("\tbind <bone-name> <bone-slot>");
                            Console.WriteLine("Unbinds a bone from its slot. Accepts either the bone name or the slot name.");
                            Console.WriteLine("\tunbind <bone-slot>");
                            Console.WriteLine();
                            Console.WriteLine("Outputs details of the entire project");
                            Console.WriteLine("\tlist summary");
                            Console.WriteLine("Outputs a list of the specified feature");
                            Console.WriteLine("\tlist bones");
                            Console.WriteLine("\tlist materials");
                            Console.WriteLine("\tlist meshes");
                            Console.WriteLine("\tlist slots");
                            Console.WriteLine("\tlist bodygroups");
                            Console.WriteLine("\tlist materialpaths");
                            Console.WriteLine("\tlist bounds");

                            Console.WriteLine();
                            Console.WriteLine("Scales the model by the specified factor.");
                            Console.WriteLine("\tscale <scale-factor>");
                            Console.WriteLine("Sets the compiled model path for the .mdl file. Relative to the addon folder.");
                            Console.WriteLine("\tmodelpath <relative-path>");
                            Console.WriteLine("Sets the target animation set to use");
                            Console.WriteLine("\tanimprofile <animation-set-name>");
                            Console.WriteLine("Valid animation sets:");
                            Console.WriteLine("\tp_male");
                            Console.WriteLine("\tp_female");
                            Console.WriteLine("\tnpc_combine");
                            Console.WriteLine("\tnpc_metrocop");
                            Console.WriteLine("\tnpc_male");
                            Console.WriteLine("\tnpc_female");
                            Console.WriteLine("\tragdoll");
                            Console.WriteLine();

                            Console.WriteLine("Adds or removes material paths to search for materials when generating the QC. Relative to the addon folder. \"\" means root materials folder.");
                            Console.WriteLine("\tmaterialpath add <relative-path>");
                            Console.WriteLine("\tmaterialpath remove <relative-path>");
                            Console.WriteLine("Sets the surfaceprop for the model, which determines footstep sounds and other material interactions in-game.");
                            Console.WriteLine("\tsurfaceprop <surface-prop-name>");
                            Console.WriteLine("Adds a bodygroup with the specified name, and adds the specified meshes to it. Optionally can specify extra meshes to add to the bodygroup.");
                            Console.WriteLine("\tbodygroup add <group-name> <mesh-name> [extra mesh] [extra mesh] ...");
                            Console.WriteLine("Removes the specified bodygroup");
                            Console.WriteLine("\tbodygroup remove <group-name>");

                            Console.WriteLine();
                            break;
                        case "import":
                            Commands.ProjectEditing.ProjectCommands.Import(commandArgs, project, Services);
                            break;
                        case "rename":
                            Commands.ProjectEditing.ProjectCommands.Rename(commandArgs, project);
                            break;
                        case "save":
                            Commands.ProjectEditing.ProjectCommands.Save(project);
                            break;
                        case "output":
                            Commands.ProjectEditing.ProjectCommands.Output(commandArgs, project);
                            break;
                        case "export":
                            Commands.ProjectEditing.ProjectCommands.Export(commandArgs, project, Services);
                            break;
                        case "exit":
                            if (commandArgs.Contains("--force"))
                            {
                                Console.WriteLine("Exiting without saving...");
                                shouldExit = true;
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Do you want to save the project before exiting? (y/n)");
                                var saveInput = Console.ReadLine()?.ToLower();
                                while (saveInput != "y" && saveInput != "n")
                                {
                                    Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
                                    saveInput = Console.ReadLine();
                                }

                                if (saveInput != null && saveInput.ToLower() == "y")
                                {
                                    if (project.Directory == null)
                                    {
                                        Console.WriteLine("Project has no directory. Please enter a directory to save the project:");
                                        var saveDir = Console.ReadLine();
                                        while (string.IsNullOrEmpty(saveDir))
                                        {
                                            Console.WriteLine("Invalid input. Please enter a valid directory:");
                                            saveDir = Console.ReadLine();
                                        }
                                        project.Directory = saveDir;
                                    }

                                    ProjectSerializer.SerializeProject(project, Path.Combine(project.Directory, project.Name + ".yggproj"));

                                    Console.WriteLine("Project saved. Exiting...");
                                    shouldExit = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Exiting without saving...");
                                    shouldExit = true;
                                    break;
                                }

                            }
                        case "scale":
                            Commands.ProjectEditing.ProjectCommands.Scale(commandArgs, project);
                            break;
                        case "modelpath":
                            Commands.ProjectEditing.ProjectCommands.ModelPath(commandArgs, project);
                            break;
                        case "bind":
                            Commands.ProjectEditing.ProjectCommands.Bind(commandArgs, project);
                            break;
                        case "unbind":
                            Commands.ProjectEditing.ProjectCommands.Unbind(commandArgs, project);
                            break;
                        case "list":
                            Commands.ProjectEditing.ProjectCommands.List(commandArgs, project);
                            break;
                        case "animprofile":
                            Commands.ProjectEditing.ProjectCommands.AnimationProfile(commandArgs, project);
                            break;
                        case "materialpath":
                            Commands.ProjectEditing.ProjectCommands.MaterialPath(commandArgs, project);
                            break;
                        case "surfaceprop":
                            Commands.ProjectEditing.ProjectCommands.SurfaceProp(commandArgs, project);
                            break;
                        case "bodygroup":
                            Commands.ProjectEditing.ProjectCommands.Bodygroup(commandArgs, project);
                            break;
                        case "bone":
                            if (args.Length == 0)
                            {
                                Console.WriteLine("No arguments provided");
                                break;
                            }
                            var boneToCheck = ArgReader.ParseFirstParameter(commandArgs);
                            if (boneToCheck == null)
                            {
                                Console.WriteLine($"Bone not provided exist");
                                break;
                            }
                            // Find the bone in the scene
                            var bone = project.Scene.RootBone?.FindBoneInChildren(boneToCheck);
                            // Output it's position etc
                            if (bone != null)
                            {
                                Console.WriteLine($"Position:   {bone.LocalPosition} | {bone.WorldPosition}");
                                Console.WriteLine($"Rotation:   {bone.LocalRotation.EulerAngles} | {bone.WorldRotation.EulerAngles}");
                                Console.WriteLine($"Scale:      {bone.LocalScale} | {bone.WorldScale}");
                            }
                            else
                            {
                                Console.WriteLine($"{boneToCheck} does not exist in the armature");
                            }


                                break;
                        default:
                            Console.WriteLine($"Unknown command: {command}. Type 'help' for a list of commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            } while (!shouldExit);

            // User has exited the loop, so we can exit the program

        }
    }
}
