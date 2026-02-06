using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;

namespace Yggdrassil.Infrastructure.Export
{
    /// <summary>
    /// Writes source DMX files from normalized mesh, skeleton and animation data.
    /// Writes to ascii DMX format, which may need to be converted to binary DMX for use in Source engine tools.
    /// </summary>
    internal class DmxExporter : IMeshExporter
    {
    }
}
