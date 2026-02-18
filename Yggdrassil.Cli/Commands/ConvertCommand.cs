using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Application.Pipeline.Steps;

namespace Yggdrassil.Cli.Commands
{
    internal class ConvertCommand
    {
        public ConvertCommand(Composition.Services services)
        {
            Services = services;
        }

        private Composition.Services Services { get; }


        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: No input file specified.");
                return 2; // Invalid arguments
            }

            // Check for help flag
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("Usage: yggdrassil convert <model-file> --out <output-directory> [--format <smd/dmx>]");
                Console.WriteLine("Converts the specified model file and outputs logs to the given directory. Attempts to convert the model to the specified format if an exporter is available internally.");
                return 0; // Success
            }

            // Parse model file argument (First non-flag argument)
            var modelFile = Parsing.ArgReader.ParseFirstParameter(args);

            if (modelFile == null)
            {
                Console.Error.WriteLine("Error: No input file specified.");
                return 2; // Invalid arguments
            }

            // Parse output directory argument
            if (!Parsing.ArgReader.ParseArgument(args, "--out", out var outputDir) || string.IsNullOrEmpty(outputDir))
            {
                Console.Error.WriteLine("Error: Output directory not specified. Use --out <output-directory>.");
                return 2; // Invalid arguments
            }

            // Parse format argument (optional)
            string? format = null;
            if (Parsing.ArgReader.ParseArgument(args, "--format", out var formatValue) && !string.IsNullOrEmpty(formatValue))
            {
                format = formatValue.ToLower();
                if (format != "smd" && format != "dmx")
                {
                    Console.Error.WriteLine("Error: Invalid format specified. Supported formats are 'smd' and 'dmx'.");
                    return 2; // Invalid arguments
                }
            }

            // Parse debug flag (optional)
            bool debug = Parsing.ArgReader.HasFlag(args, "--debug");


            IMeshExporter exporter = format switch
            {
                "smd" => Services.SmdExporter ?? Services.GeneralExporter,
                "dmx" => Services.DmxExporter ?? Services.GeneralExporter,
                _ => Services.GeneralExporter
            };

            try
            {
                var sceneModel = await Services.Importer.ImportModelAsync(modelFile);
                var exportPath = Path.GetFullPath(outputDir);

                Console.WriteLine($"Converting model '{modelFile}' to format '{format ?? "default"}'...");
                await exporter.ExportSceneAsync(exportPath, sceneModel);
                Console.WriteLine($"Model converted successfully. Output saved to: {exportPath}");
                

                // If debug, also export the scenemodel as JSON for inspection
                if (debug)
                {
                    var outputPath = Path.Combine(exportPath, "imported_scene.txt");
                    await File.WriteAllTextAsync(outputPath, sceneModel?.ToString(), cancellationToken);
                }

            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Error: An exception occurred during conversion. {exception.Message}");
                return 1; // General error
            }

            return 0; // Success
        }
    }
}
