using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    ///  Exports normalized meshes and skeletons to the output directory.
    ///  Exports to SMD/VTA or DMX.
    /// </summary>
    public class ExportMeshStep : IBuildStep
    {
        public string Name => "Export Meshes";


        public readonly IMeshExporter _exporter;


        public ExportMeshStep(IMeshExporter exporter)
        {
            _exporter = exporter;
        }

        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            if (context.Project.Scene is null)
            {
                result.Messages.Add(new BuildMessage(BuildSeverity.Error, "export", "No scene available for ExportMeshStep"));
                return Task.CompletedTask;
            }


            _exporter.ExportSceneAsync(context.Paths.OutputDirectory, context.Project.Scene);

            result.Messages.Add(new BuildMessage(BuildSeverity.Info, "export", "Meshes exported successfully."));
            return Task.CompletedTask;

        }
    }
}
