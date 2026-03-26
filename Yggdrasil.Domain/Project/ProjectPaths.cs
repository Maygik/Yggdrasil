using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrasil.Domain.Project
{
    /// <summary>
    /// Centralizes all project-relative paths used in various parts of the application.
    /// Prevents strongly-typed path representations from leaking outside of this assembly.
    /// </summary>
    public sealed class ProjectPaths
    {
        public string OutputDirectory { get; } = "";


        public ProjectPaths(string outputRoot)
        {
            OutputDirectory = Path.GetFullPath(outputRoot);
        }

        // Folders
        public string StudioMdlDir => Path.Combine(OutputDirectory, "studiomdl");
        public string LogsDir => Path.Combine(OutputDirectory, "logs");
        public string MeshDir => Path.Combine(OutputDirectory, "meshes");
        public string AnimDir => Path.Combine(OutputDirectory, "anims");

        public string QcFile => Path.Combine(StudioMdlDir, "model.qc");

        public string QcMeshPath(string fileName) => $"meshes/{fileName}";
        public string QcAnimPath(string fileName) => $"anims/{fileName}";

        public static string NormalizeQcPath(string path) => path.Replace('\\', '/');
    }
}
