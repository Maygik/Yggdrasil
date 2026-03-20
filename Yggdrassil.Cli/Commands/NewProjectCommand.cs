using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application;
using Yggdrassil.Application.UseCases;

namespace Yggdrassil.Cli.Commands
{
    internal class NewProjectCommand
    {
        public NewProjectCommand(AppServices services)
        {
            Services = services;
        }

        private AppServices Services { get; }

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
                var request = new CreateProjectRequest
                {
                    Name = projectName,
                    ProjectDirectory = projectDirectory
                };
                var result = Services.CreateProject.Execute(request);
                EditingProjectCommand.PrintServiceResult(result);

                if (result.Success && result.CreatedProject != null)
                {
                    EditingProjectCommand.EditProject(result.CreatedProject, Services);
                }
                else
                {
                    Console.Error.WriteLine($"Project could not be created at '{result.ProjectFilePath}'.");
                    return 1;
                }


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
