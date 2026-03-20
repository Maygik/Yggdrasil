using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Cli.Commands
{
    internal class HelpCommand
    {
        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            // new <project-name> <project-directory>
            // open <project-file>
            // help


            Console.WriteLine("Yggdrassil CLI - Help");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  ygg [command] [options]");
            Console.WriteLine();
            Console.WriteLine("Available Commands:");
            Console.WriteLine("  new <project-name> <project-directory>   Create a new project");
            Console.WriteLine("  open <project-file>                       Open an existing project");
            Console.WriteLine("  help                                      Show this help message");
            Console.WriteLine();
            return await Task.FromResult(0); // Success
        }
    }
}
