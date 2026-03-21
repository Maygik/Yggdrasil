using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Presentation.Models
{
    public sealed class RecentProjectEntry
    {
        public required string Name { get; init; }
        public required string FilePath { get; init; }
        public DateTimeOffset LastOpened { get; init; }
        public bool IsMissing { get; init; } = false;
    }
}
