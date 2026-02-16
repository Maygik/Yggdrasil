using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Cli.Commands;
using Yggdrassil.Cli.Composition;

namespace Yggdrassil.Cli
{
    public class CliApp
    {
        /// <summary>
        /// Runs the CLI application with the provided command-line arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>
        /// Returns an integer exit code indicating the result of the operation:
        /// Return Codes:
        /// 0: Success
        /// 1: General error
        /// 2: Invalid arguments
        /// 130: Process terminated by Ctrl+C (cancellation requested)
        /// 3: Build failed (e.g. due to errors in the project file or during processing)
        /// 4: Import failed (e.g. due to errors in the source file or during processing)
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> RunAsync(string[] args)
        {
            // Setup cancellation handling for Ctrl+C
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Cancellation requested...");
                cts.Cancel();
                eventArgs.Cancel = true; // Prevent the process from terminating immediately
            };

            // If no arguments provided, return usage information
            if (args.Length == 0)
            {
                // No arguments provided, show usage information
                PrintUsage();
                return 2;
            }


            try
            {
                var services = Services.Create();

                var cmd = args[0].ToLowerInvariant(); // Get the command (e.g., "build")
                var rest = args.Skip(1).ToArray(); // Get the rest of the arguments (e.g., project file and options)

                return cmd switch
                {
                    "help" => await new HelpCommand().RunAsync(rest, cts.Token),
                    "build" => await new BuildCommand(services).RunAsync(rest, cts.Token),
                    "import" => await new ImportCommand(services).RunAsync(rest, cts.Token),
                    _ => 2 // Unknown command. Return code 2 for invalid arguments.
                };

            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled by user.");
                return 130;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }



        private void PrintUsage()
        {
            Console.WriteLine("Yggdrassil CLI - Usage:");
            Console.WriteLine("  yggdrassil <command> [options]");
            Console.WriteLine();
            // Tell the user the help command is available
            Console.WriteLine("Use 'yggdrassil help' to see available commands.");
            Console.WriteLine();
        }
    }
}
