using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Application.Pipeline
{
    /// <summary>
    ///  Holds transient data for a single pipeline build operation.
    ///  (Project snapshot, temp folders, imported scene etc.)
    ///  Exists only for the duration of a single build.
    /// </summary>
    public sealed class BuildContext
    {
        public required Project Project { get; init; }
        public required ProjectPaths Paths { get; init; }

        public BuildContext Clone()
        {
            // Create a deep copy of the BuildContext to ensure that modifications to the clone do not affect the original context.
            return new BuildContext
            {
                Project = this.Project, // Assuming Project is immutable or a reference type that doesn't need deep copying. If not, implement a proper deep copy if necessary.
                Paths = new ProjectPaths(Paths.OutputDirectory)
            };
        }
    }
}
