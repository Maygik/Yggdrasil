using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application;
using Yggdrasil.Cli.Parsing;
using Yggdrasil.Domain.Project;
using Yggdrasil.Application.UseCases;

namespace Yggdrasil.Cli.Commands
{
    public class EditingProjectCommand
    {
        public static void PrintServiceResult(ServiceResult result)
        {
            foreach (var message in result.Messages)
            {
                Console.WriteLine(message);
            }

            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"Warning: {warning}");
            }

            if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }

        private static void PrintProjectHelp()
        {
            Console.WriteLine("Project Commands");
            Console.WriteLine();

            Console.WriteLine("Project");
            Console.WriteLine("  import <model-file> [--automap]     Import a model into the project.");
            Console.WriteLine("  save                                Save the current project.");
            Console.WriteLine("  export <all|qc|mesh> [--out <dir>] [--format <smd|dmx>]");
            Console.WriteLine("                                      Export QC, meshes, or both.");
            Console.WriteLine("  exit [--force]                      Exit the project editor.");
            Console.WriteLine();

            Console.WriteLine("Settings");
            Console.WriteLine("  rename <new-project-name>           Rename the project.");
            Console.WriteLine("  output <output-directory>           Set the export output directory.");
            Console.WriteLine("  scale <scale-factor>                Scale the imported model.");
            Console.WriteLine("  modelpath <relative-path>           Set the compiled model path.");
            Console.WriteLine("  animprofile <profile>               Set the target animation profile.");
            Console.WriteLine("  surfaceprop <surface-prop>          Set the QC surface prop.");
            Console.WriteLine();

            Console.WriteLine("Rigging");
            Console.WriteLine("  bind <bone-name> <bone-slot>        Bind a source bone to a rig slot.");
            Console.WriteLine("  unbind <bone-slot>                  Clear a rig slot binding.");
            Console.WriteLine("  bone <bone-name>                    Show transform details for one bone.");
            Console.WriteLine();

            Console.WriteLine("Materials And Bodygroups");
            Console.WriteLine("  materialpath add <relative-path>    Add a QC material search path.");
            Console.WriteLine("  materialpath remove <relative-path> Remove a QC material search path.");
            Console.WriteLine("  bodygroup add <name> <mesh...>      Add a bodygroup and its meshes.");
            Console.WriteLine("  bodygroup remove <name>             Remove a bodygroup.");
            Console.WriteLine();

            Console.WriteLine("List");
            Console.WriteLine("  list summary");
            Console.WriteLine("  list bones");
            Console.WriteLine("  list materials");
            Console.WriteLine("  list meshes");
            Console.WriteLine("  list slots");
            Console.WriteLine("  list bodygroups");
            Console.WriteLine("  list materialpaths");
            Console.WriteLine("  list bounds");
            Console.WriteLine();

            Console.WriteLine("Animation Profiles");
            Console.WriteLine("  p_male, p_female, npc_combine, npc_metrocop, npc_male, npc_female, ragdoll");
            Console.WriteLine();
        }

        public static void EditProject(Project project, AppServices Services)
        {
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
                            PrintProjectHelp();
                            break;
                        case "import":
                            Commands.ProjectEditing.ProjectCommands.Import(commandArgs, project, Services);
                            break;
                        case "rename":
                            Commands.ProjectEditing.ProjectCommands.Rename(commandArgs, project, Services);
                            break;
                        case "save":
                            Commands.ProjectEditing.ProjectCommands.Save(project, Services);
                            break;
                        case "output":
                            Commands.ProjectEditing.ProjectCommands.Output(commandArgs, project, Services);
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

                                    var saveResult = Services.SaveProject.Execute(new SaveProjectRequest
                                    {
                                        Project = project
                                    });
                                    PrintServiceResult(saveResult);

                                    if (!saveResult.Success)
                                    {
                                        break;
                                    }

                                    Console.WriteLine("Exiting...");
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
                            Commands.ProjectEditing.ProjectCommands.Scale(commandArgs, project, Services);
                            break;
                        case "modelpath":
                            Commands.ProjectEditing.ProjectCommands.ModelPath(commandArgs, project, Services);
                            break;
                        case "bind":
                            Commands.ProjectEditing.ProjectCommands.Bind(commandArgs, project, Services);
                            break;
                        case "unbind":
                            Commands.ProjectEditing.ProjectCommands.Unbind(commandArgs, project, Services);
                            break;
                        case "list":
                            Commands.ProjectEditing.ProjectCommands.List(commandArgs, project);
                            break;
                        case "animprofile":
                            Commands.ProjectEditing.ProjectCommands.AnimationProfile(commandArgs, project, Services);
                            break;
                        case "materialpath":
                            Commands.ProjectEditing.ProjectCommands.MaterialPath(commandArgs, project, Services);
                            break;
                        case "surfaceprop":
                            Commands.ProjectEditing.ProjectCommands.SurfaceProp(commandArgs, project, Services);
                            break;
                        case "bodygroup":
                            Commands.ProjectEditing.ProjectCommands.Bodygroup(commandArgs, project, Services);
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
