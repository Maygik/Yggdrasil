using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    ///  Generates source .smd files for all animations, includes proportion trick if enabled
    /// </summary>
    public class ExportAnimationsStep : IBuildStep
    {
        public string Name => "Export Animations";
        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}