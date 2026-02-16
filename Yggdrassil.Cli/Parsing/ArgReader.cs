using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Cli.Parsing
{
    public static class ArgReader
    {
        // Generic method to parse an argument value by its name (e.g., --out)
        public static bool ParseArgument(string[] args, string argumentName, out string? value)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    value = args[i + 1];
                    return true;
                }
            }
            value = null;
            return false;
        }



        // Method to parse the project file path from the arguments.
        // Assuming it's the first argument after the command (e.g., "build").
        public static string? ParseFirstParameter(string[] args)
        {
            // Assuming the project file is the first argument after the command (e.g., "build")
            if (args.Length > 0)
            {
                return args[0];
            }
            return null;
        }


    }
}
