using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.QC;
using Yggdrassil.Domain.Rigging;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Domain.Project
{
    /// <summary>
    /// Serializable representation of a porting project.
    /// Owns references to import settings, materials, rig mapping, QC Config and target profile
    /// </summary>
    public sealed class Project
    {
        public string Name { get; set; } = "MyProject";
        public string? Directory { get; set; } = null; // The directory where the project file is located.
        public QcConfig Qc { get; set; } = new();
        public BuildSettings Build { get; set; } = new();

        public SceneModel Scene { get; set; } = new(); // The imported model, containing meshes and skeleton.
        public SourceBoneMapping RigMapping { get; set; } = new(); // Maps bones in the scene to source engine bones

        public bool Save()
        {
            if (Directory == null)
            {
                Console.WriteLine("Project directory is not set. Cannot save project.");
                return false;
            }
            try
            {
                var directory = Directory + ".yggproj";
                var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Saving project to {directory}...");
                System.IO.File.WriteAllText(directory, json);
                Console.WriteLine("Project saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save project: {ex.Message}");
                return false;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Project: {Name}");
            sb.AppendLine("QC Config:");
            sb.AppendLine(Qc.ToString());
            sb.AppendLine("Build Settings:");
            sb.AppendLine(Build.ToString());
            sb.AppendLine("Rig Mapping:");
            sb.AppendLine(RigMapping.ToString());

            // Append mesh names
            if (Scene != null)
            {
                sb.AppendLine($"Scene: {Scene.Name}");
                foreach (var mg in Scene.MeshGroups)
                {
                    sb.AppendLine($"\tMesh Group: {mg.Name}");
                }
            }

            return sb.ToString();
        }
    }

    public sealed class BuildSettings
    {
        public string OutputDirectory { get; set; } = "out/";

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Output Directory: {OutputDirectory}");
            return sb.ToString();
        }
    }
}
