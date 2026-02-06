using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    /// Converts MaterialIntent definitions into actual VMTs and VTFs, writing them to the output directory.
    /// Delegates image processing to IMaterialBaker (e.g. texture resizing, baking specular maps into normal maps, etc).
    /// </summary>
    public class BakeMaterialStep : IBuildStep
    {
        public string Name => "Bake Materials";
        public Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
