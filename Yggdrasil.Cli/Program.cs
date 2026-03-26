// Command-line entry point for running the import and build pipeline without UI.
// Used for initial validation and automation

namespace Yggdrasil.Cli;

internal static class Program
{
    // Simple entry point that delegates to CliApp for processing command-line arguments and running the pipeline.
    public static async Task<int> Main(string[] args)
        => await new CliApp().RunAsync(args);
}