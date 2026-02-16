using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Yggdrassil.Domain.Project;

namespace Yggdrassil.Application.UseCases
{
    /// <summary>
    /// Helper class for loading and saving project files in JSON format.
    /// </summary>
    public static class JSonIO
    {
        public static Project LoadProject(string projectFilePath)
        {
            // For simplicity, we assume the project file is a JSON file that can be deserialized into a Project object.
            if (!File.Exists(projectFilePath))
            {
                throw new FileNotFoundException($"Project file not found: {projectFilePath}");
            }

            string projectJson = File.ReadAllText(projectFilePath);
            Project? project = System.Text.Json.JsonSerializer.Deserialize<Project>(projectJson, new System.Text.Json.JsonSerializerOptions
            {
                Converters =
                {
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                }
            });
            if (project == null)
            {
                throw new InvalidDataException($"Failed to deserialize project file: {projectFilePath}");
            }
            return project;
        }

        public static void Save(string projectFilePath, Domain.Project.Project project)
        {
            // For simplicity, we serialize the Project object to JSON and save it to the specified file path.
            string projectJson = System.Text.Json.JsonSerializer.Serialize(project, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(projectFilePath, projectJson, new UTF8Encoding(false));
        }
    }
}
