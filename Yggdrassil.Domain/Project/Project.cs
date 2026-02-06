using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.QC;

namespace Yggdrassil.Domain.Project
{
    /// <summary>
    /// Serializable representation of a porting project.
    /// Owns references to import settings, materials, rig mapping, QC Config and target profile
    /// </summary>
    public sealed class Project
    {
        public string Name { get; set; } = "MyProject";
        public QcConfig Qc { get; set; } = new();
        public BuildSettings Build { get; set; } = new();
    }

    public sealed class BuildSettings
    {
        public string OutputDirectory { get; set; } = "";
    }
}
