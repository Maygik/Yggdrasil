using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    /// Normalizes imported scene data for source engine requirements.
    /// Scale, axis orientation, bone naming, etc.
    /// Does not write any files.
    /// </summary>
    public class NormalizeStep : IBuildStep
    {
        public string Name => "Normalize Model";
        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
