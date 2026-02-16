using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Infrastructure.Import;

namespace Yggdrassil.Cli.Commands
{
    internal class ImportCommand
    {
        public ImportCommand(Composition.Services services)
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
                Console.WriteLine("Usage: yggdrassil import <model-file> --out <output-directory>");
                Console.WriteLine("Imports the specified model file and outputs logs to the given directory.");
                return 0; // Success
            }

            // Parse project file argument (First non-flag argument)
            var modelFile = Parsing.ArgReader.ParseFirstParameter(args);
            if (string.IsNullOrEmpty(modelFile))
            {
                Console.Error.WriteLine("Error: No model file specified.");
                return 2; // Invalid arguments
            }

            // Parse output directory argument
            if (!Parsing.ArgReader.ParseArgument(args, "--out", out var outputDirectory) || string.IsNullOrEmpty(outputDirectory))
            {
                Console.Error.WriteLine("Error: No output directory specified. Use --out <output-directory>.");
                return 2; // Invalid arguments
            }

            try
            {
                var sceneModel = await Services.Importer.ImportModelAsync(modelFile);
                Console.WriteLine("Import completed successfully.");

                // Write a debug output file that's just the scene as a string

                var outputPath = Path.Combine(outputDirectory, "imported_scene.txt");
                await File.WriteAllTextAsync(outputPath, sceneModel?.ToString(), cancellationToken);

                return 0; // Success
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during import: {ex.Message}");
                return 1; // General error
            }
        }
    }
}
