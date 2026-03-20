using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application;
using Yggdrassil.Cli.Commands;
using Yggdrassil.Cli.Parsing;
using Yggdrassil.Infrastructure;

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

            var services = AppServicesFactory.Create();

            if (args.Length == 0)
                return await RunLauncherAsync(services, cts.Token);

            if (IsProjectFileLaunch(args))
            {
                // If the first argument looks like a project file, treat it as an "open" command
                return await new OpenProjectCommand(services).RunAsync(args, cts.Token);
            }


            return await DispatchCommandAsync(args, services, cts.Token);
        }

        private bool IsProjectFileLaunch(string[] args)
        {
            if (args.Length == 0)
                return false;
            var firstArg = args[0];
            return firstArg.EndsWith(".yggproj", StringComparison.OrdinalIgnoreCase) ||
                   firstArg.EndsWith(".yggproj.json", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int> RunLauncherAsync(AppServices services, CancellationToken token)
        {
            while (true)
            {
                Console.Write("ygg> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var inputArgs = ArgReader.ExtractArguments(input);
                var command = inputArgs[0].ToLowerInvariant();

                if (command == "exit" || command == "quit")
                {
                    Console.WriteLine("Exiting Yggdrassil CLI.");
                    return 0;
                }

                var exitCode = await DispatchCommandAsync(inputArgs, services, token);
            }
        }

        private async Task<int> DispatchCommandAsync(string[] args, AppServices services, CancellationToken token)
        {
            if (args.Length == 0)
                return 2;

            var cmd = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();

            return cmd switch
            {
                "help" => await new HelpCommand().RunAsync(rest, token),
                "new" => await new NewProjectCommand(services).RunAsync(rest, token),
                "open" => await new OpenProjectCommand(services).RunAsync(rest, token),
                _ => 2 // Unknown command. Return code 2 for invalid arguments.
            };
        }
    }
}
