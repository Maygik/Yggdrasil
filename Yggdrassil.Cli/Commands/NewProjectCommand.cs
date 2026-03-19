using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Infrastructure.Serialization;

namespace Yggdrassil.Cli.Commands
{
    internal class NewProjectCommand
    {
        public NewProjectCommand(Composition.Services services)
        {
            Services = services;
        }

        private Composition.Services Services { get; }

        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: No project name specified.");
                return 2; // Invalid arguments
            }
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Error: Not enough arguments.");
                Console.WriteLine("Usage: yggdrassil new <project-name> <project-path>");
                return 2; // Invalid arguments
            }
            // Check for help flag
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("Usage: yggdrassil new <project-name> <project-path>");
                Console.WriteLine("Creates a new Yggdrassil project with the specified name.");
                return 0; // Success
            }
            var projectName = Parsing.ArgReader.ParseFirstParameter(args);
            if (string.IsNullOrEmpty(projectName))
            {
                Console.Error.WriteLine("Error: No project name specified.");
                return 2; // Invalid arguments
            }
            var projectDirectory = Parsing.ArgReader.ParseNParameter(args, 2);
            try
            {
                if (!Directory.Exists(projectDirectory))
                {
                    Directory.CreateDirectory(projectDirectory);
                }


                var projectFilePath = Path.Combine(projectDirectory, $"{projectName}.yggproj");
                
                var project = new Domain.Project.Project
                {
                    Name = projectName,
                    Directory = projectDirectory,
                };
                project.Build.OutputDirectory = projectDirectory + "/output";
                try
                {
                    Services.ProjectStore.Save(projectFilePath, project);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error saving project file: {ex.Message}");
                    return 1; // General error
                }

                Console.WriteLine($"Project '{projectName}' created successfully at '{projectDirectory}'.");
                EditingProjectCommand.EditProject(project, Services);
                return 0; // Success
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating project: {ex.Message}");
                return 1; // General error
            }
        }
    }
}
