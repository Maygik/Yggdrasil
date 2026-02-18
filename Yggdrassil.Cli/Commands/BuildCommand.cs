using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Pipeline;
using Yggdrassil.Application.Pipeline.Steps;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Cli.Parsing;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Cli.Commands
{
    internal class BuildCommand
    {
        public BuildCommand(Composition.Services services)
        {
            Services = services;
        }

        private Composition.Services Services { get; }

        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: No project file specified.");
                return 2; // Invalid arguments
            }

            // Check for help flag
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("Usage: yggdrassil build <project-file> --out <output-directory>");
                Console.WriteLine("Builds the specified project file and outputs to the given directory.");
                return 0; // Success
            }


            // Parse project file argument (First non-flag argument)
            var projectFile = ArgReader.ParseFirstParameter(args);
            if (string.IsNullOrEmpty(projectFile))
            {
                Console.Error.WriteLine("Error: No project file specified.");
                return 2; // Invalid arguments
            }


            try
            {
                // Validate other arguments

                // Requires output directory argument
                if (!ArgReader.ParseArgument(args, "--out", out var outputDir) || string.IsNullOrEmpty(outputDir))
                {
                    Console.Error.WriteLine("Error: Output directory not specified.");
                    return 2; // Invalid arguments
                }


                var pipeline = new BuildPipeline();
                pipeline.AddStep(new GenerateQCStep(Services.Assembler));
                pipeline.AddStep(new ExportMeshStep(Services.GeneralExporter));
                pipeline.AddStep(new WriteBuildLogStep());

                // Load the specified project file
                Project project = JSonIO.LoadProject(projectFile);

                // Setup context for project and paths
                var context = new BuildContext
                {
                    Project = project,
                    Paths = new ProjectPaths(outputDir)
                };


                var result = await pipeline.RunAsync(context, cancellationToken);

                foreach (var message in result.Messages)
                {
                    Console.WriteLine($"{message.Severity}: {message.Message}");
                }

                return result.Success ? 0 : 3; // Return 0 for success, 3 for build errors
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Build failed: {ex.Message}");
                return 3; // Build failed
            }
        }
    }
}
