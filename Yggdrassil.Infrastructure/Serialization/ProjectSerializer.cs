using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Infrastructure.Serialization
{
    public static class ProjectSerializer
    {
        // Serializes the project to a JSON file
        // Does not serialize the SceneModel's SceneData to avoid bloating the JSON file
        public static void SerializeProject(Project project, string filePath)
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    Converters =
                {
                    new System.Text.Json.Serialization.JsonStringEnumConverter(),
                    new Matrix4x4JsonConverter(),
                }
                };


                // Create a copy of the project without the SceneData

                var projectCopy = new Project
                {
                    Name = project.Name,
                    Directory = project.Directory,
                    Qc = project.Qc,
                    Build = project.Build,
                    Scene = null!,
                    RigMapping = project.RigMapping
                };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(projectCopy, options);

                filePath = Path.ChangeExtension(filePath, ".yggproj");

                System.IO.File.WriteAllText(filePath, jsonString);

                Console.WriteLine($"Project serialized to {filePath}");

                filePath = Path.GetDirectoryName(filePath) ?? throw new Exception("Failed to get directory from file path.");

                Console.WriteLine($"Serializing mesh data to {Path.Combine(filePath, project.Name + ".yggmdl")}");


                SceneModelSerializer.SerializeSceneModel(project.Scene, Path.Combine(filePath, project.Name + ".yggmdl"));
                Console.WriteLine($"Mesh data serialized to {Path.Combine(filePath, project.Name + ".yggmdl")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing project: {ex.Message}");
                return;
            }

        }

        public static Project DeserializeProject(string filePath)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText(filePath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    IncludeFields = true,
                    Converters =
                    {
                        new System.Text.Json.Serialization.JsonStringEnumConverter(),
                        new Matrix4x4JsonConverter()
                    }
                };
                var project = System.Text.Json.JsonSerializer.Deserialize<Project>(jsonString, options);
                if (project == null)
                {
                    throw new Exception("Failed to deserialize project from JSON.");
                }

                // Deserialize the SceneModel from the .yggmdl file
                string sceneFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? throw new Exception("Failed to get directory from file path."), project.Name + ".yggmdl");
                if (System.IO.File.Exists(sceneFilePath))
                {
                    project.Scene = SceneModelSerializer.Deserialize(sceneFilePath);
                }
                else
                {
                    Console.WriteLine($"Warning: Scene file {sceneFilePath} not found. Scene will be null.");
                    project.Scene = null!;
                }


                return project;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing project: {ex.Message}");
                throw;
            }
        }
    }
}
