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
            Console.WriteLine("Yggdrassil CLI - Help");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  yggdrassil <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  build <project.json> --out <folder>   Build the project specified by the JSON file and output to the folder.");
            Console.WriteLine("  import <model.obj> --out <folder>    Import the specified model file and output to the folder.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --out <folder>    Specify the output directory for the build artifacts.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  yggdrassil build myproject.json --out ./output");
            Console.WriteLine();
            Console.WriteLine("  yggdrassil import mymodel.obj --out ./output");
            Console.WriteLine();
            return await Task.FromResult(0); // Success
        }
    }
}
