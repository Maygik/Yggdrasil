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
    }
}
