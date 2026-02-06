using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    ///  Exports normalized meshes and skeletons to the output directory.
    ///  Exports to SMD/VTA or DMX.
    /// </summary>
    public class ExportMeshStep : IBuildStep
    {
        public string Name => "Export Meshes";
        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
