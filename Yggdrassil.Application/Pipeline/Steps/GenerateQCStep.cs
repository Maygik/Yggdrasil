using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    /// Assembles the final QC and QCIs based on QcConfig, animation profile and features.
    /// </summary>
    public class GenerateQCStep : IBuildStep
    {
        private readonly IQcAssembler _assembler;

        public string Name => "Generate QC";

        public GenerateQCStep(IQcAssembler assembler)
        {
            _assembler = assembler;
        }

        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Build the QC text
            var qcText = _assembler.AssembleQc(context.Project.Qc);

            var qcPath = context.Paths.QcFile;
            File.WriteAllText(qcPath, qcText, new UTF8Encoding(false));
            result.Artifacts.Add(new BuildArtifact(qcPath, "qc"));

            return Task.CompletedTask;
        }
    }
}
