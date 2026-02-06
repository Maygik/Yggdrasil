using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline.Steps
{
    /// <summary>
    ///  Defines a single deterministic step in the model export pipeline.
    ///  Transforms build context into artifacts or modifies context state.
    /// </summary>
    public interface IBuildStep
    {
        string Name { get; }
        Task RunAsync(BuildContext context, BuildResult result, CancellationToken cancellationToken);
    }
}
