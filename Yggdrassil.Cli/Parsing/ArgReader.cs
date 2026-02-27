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
                    value = NormalizePathArgument(args[i + 1]);
                    return true;
                }
            }
            value = null;
            return false;
        }


        public static string[] ExtractArguments(string argumentString)
        {
            // Extracts arguments from a command line string, handling quoted arguments and slashes properly.
            var args = new List<string>();
            var currentArg = new StringBuilder();
            bool inQuotes = false;
            foreach (var c in argumentString)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes; // Toggle the inQuotes flag
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentArg.Length > 0)
                    {
                        args.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            // Add the last argument if there is one
            if (currentArg.Length > 0)
            {
                args.Add(currentArg.ToString());
            }

            return args.ToArray();
        }


        // Method to parse the project file path from the arguments.
        // Assuming it's the first argument after the command (e.g., "build").
        public static string? ParseFirstParameter(string[] args)
        {
            // Parse the first parameter, accounting for "" and '' around the argument, and normalizing path separators.
            // Assuming the project file is the first argument after the command (e.g., "build")
            if (args.Length > 0)
            {
                // Return that normalised
                return NormalizePathArgument(args[0]);
            }
            return null;
        }

        public static string? ParseNParameter(string[] args, int n)
        {
            // Assuming the project file is the first argument after the command (e.g., "build")
            if (args.Length >= n)
            {
                return NormalizePathArgument(args[n - 1]);
            }
            return null;
        }


        public static bool HasFlag(string[] args, string flagName)
        {
            return args.Any(arg => arg.Equals(flagName, StringComparison.OrdinalIgnoreCase));
        }

        // Normalize path arguments by removing quotes and standardizing separators
        private static string NormalizePathArgument(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return argument;
            }

            // Remove surrounding quotes (single or double)
            string normalized = argument.Trim();
            if ((normalized.StartsWith('"') && normalized.EndsWith('"')) ||
                (normalized.StartsWith('\'') && normalized.EndsWith('\'')))
            {
                normalized = normalized[1..^1];
            }

            // Normalize path separators to the current platform's separator
            normalized = normalized.Replace('\\', Path.DirectorySeparatorChar)
                                  .Replace('/', Path.DirectorySeparatorChar);

            return normalized;
        }
    }
}
