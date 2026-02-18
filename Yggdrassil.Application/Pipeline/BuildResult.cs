using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline
{
    public enum BuildSeverity { Info, Warning, Error }

    public sealed record BuildMessage(BuildSeverity Severity, string Code, string Message);
    // Code examples: "qc", "qci", "log"

    public sealed class BuildResult
    {
        public List<BuildArtifact> Artifacts { get; } = new();
        public List<BuildMessage> Messages { get; } = new();

        public bool Success => Messages.All(m => m.Severity != BuildSeverity.Error);
    }
}
