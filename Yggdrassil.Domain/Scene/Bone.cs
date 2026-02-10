using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Scene
{
    /// <summary>
    /// Represents a single bone in a skeleton hierarchy.
    /// Includes transformation data and parent-child relationships.
    /// Does not perform animation or transform logic.
    /// </summary>
    public class Bone : Transform
    {
        public string Name { get; set; } = string.Empty;
    }
}
