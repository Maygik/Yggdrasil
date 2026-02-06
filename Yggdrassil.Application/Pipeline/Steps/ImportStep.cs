using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    /// Imports the model using IModelImporter and stores the result in the build context.
    /// </summary>
    public class ImportStep : IBuildStep
    {
        public string Name => "Import Model";
        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
