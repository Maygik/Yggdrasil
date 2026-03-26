using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Yggdrasil.Application;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;

namespace Yggdrasil.Cli.Commands
{
    internal class OpenProjectCommand
    {
        public OpenProjectCommand(AppServices services)
        {
            Services = services;
        }

        private AppServices Services { get; }

        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            // Check for help flag
            if (args.Contains("--help") || args.Contains("-h"))
            {
            Console.WriteLine("Usage: yggdrasil open <project-file-path>");
                Console.WriteLine("Opens an existing Yggdrasil project from the specified file path.");
                return 0; // Success
            }

            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: No project file path specified.");
                return 2; // Invalid arguments
            }

            // The project file path is the first argument
            var projectFilePath = Parsing.ArgReader.ParseFirstParameter(args);
            if (string.IsNullOrEmpty(projectFilePath))
            {
                Console.Error.WriteLine("Error: No project file path specified.");
                return 2; // Invalid arguments
            }
            if (!File.Exists(projectFilePath))
            {
                Console.Error.WriteLine($"Error: The specified project file '{projectFilePath}' does not exist.");
                return 3; // File not found
            }

            Project? project;

            // Load project
            try
            {
                var request = new OpenProjectRequest { ProjectFilePath = projectFilePath };
                var result = Services.OpenProject.Execute(request);
                EditingProjectCommand.PrintServiceResult(result);

                if (!result.Success)
                {
                    return 5; // Failed to parse project
                }

                project = result.OpenedProject;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to open project file. {ex.Message}");
                return 4; // Failed to open project
            }

            if (project == null)
            {
                Console.Error.WriteLine("Error: Failed to load project. The file may be corrupted or in an invalid format.");
                return 5; // Failed to parse project
            }



            EditingProjectCommand.EditProject(project, Services);
            return 0; // Success
        }
    }
}
