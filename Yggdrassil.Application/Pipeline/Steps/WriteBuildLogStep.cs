using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    public sealed class WriteBuildLogStep : IBuildStep
    {
        public string Name => "Write Build Log";
        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sb = new StringBuilder();
            sb.AppendLine($"Project: {context.Project.Name}");
            sb.AppendLine($"Output: {context.Paths.OutputDirectory}");
            sb.AppendLine();

            foreach (var message in result.Messages)
            {
                sb.AppendLine($"[{message.Severity}] {message.Message}");
            }

            if (result.Messages.Count == 0)
            {
                sb.AppendLine("No messages.");
            }

            sb.AppendLine();

            sb.AppendLine("Artifacts:");
            foreach (var artifact in result.Artifacts)
            {
                sb.AppendLine($"- {artifact.Kind}: {artifact.RelativePath}");
            }

            var logPath = Path.Combine(context.Paths.LogsDir, "build.log");
            File.WriteAllText(logPath, sb.ToString(), new UTF8Encoding(false));
            result.Artifacts.Add(new BuildArtifact(logPath, "log"));

            return Task.CompletedTask;
        }
    }
}
