// Command-line entry point for running the import and build pipeline without UI.
// Used for initial validation and automation

using Yggdrassil.Application.Abstractions;
using Yggdrassil.Application.Pipeline;
using Yggdrassil.Application.Pipeline.Steps;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Domain.Project;
using Yggdrassil.Infrastructure.QC;

static string? ParseOutDir(string[] args)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals("--out", StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}


// Basic command-line parsing. For simplicity, we only support "build <project.json> --out <folder>".
if (args.Length < 2 || !args[0].Equals("build", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Usage: yggdrassil build <project.json> --out <folder");
    return 1;
}

    

// Get the project path and output directory from the command-line arguments.
var projectPath = args[1];
var outputDir = ParseOutDir(args) ?? Path.Combine(Path.GetDirectoryName(projectPath)!, "out");

// Load the project file.
var project = JSonIO.Load(projectPath);

// Initialize the QC template store to ensure templates are available
QcTemplateStore store = new QcTemplateStore();
store.Init();


// Setup an assembler to create the QC
var assembler = new QcAssembler(store);

// Setup the pipline
var pipeline = new BuildPipeline();
pipeline.AddStep(new GenerateQCStep(assembler));
// TODO: Add more steps here for generating materials, copying model files, etc. For now, we just generate the QC and write the build log.
pipeline.AddStep(new WriteBuildLogStep());


// Setup context for project and paths
var context = new BuildContext
{
    Project = project,
    Paths = new ProjectPaths(outputDir)
};

// Cancellation handling for ctrl+c
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Cancellation requested...");
    cancellationTokenSource.Cancel();
    e.Cancel = true; // prevent immediate process termination
};

// Run the pipeline to create the files and log. Allows cancellation via Ctrl+C.
var result = await pipeline.RunAsync(context, cancellationTokenSource.Token);

// Debug output of build messages and final status.
foreach (var message in result.Messages)
{
    Console.WriteLine($"[{message.Severity}] {message.Message}");
}
Console.WriteLine(result.Success ? "Build succeeded." : "Build failed.");
return result.Success ? 0 : 1;