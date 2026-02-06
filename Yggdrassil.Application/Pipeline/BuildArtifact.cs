using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline
{
    public sealed record BuildArtifact(string RelativePath, string Kind);
}
