using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Domain.Project;
using Yggdrasil.Infrastructure.Serialization;

namespace Yggdrasil.Infrastructure.IO
{
    public class YggProjectStore : IProjectStore
    {
        public Project? LoadProject(string projectFilePath)
        {
            // Open the file and Deserialize using ProjectSerializer.Deserialize
            try
            {
                var project = ProjectSerializer.DeserializeProject(projectFilePath);
                return project;
            }
            catch(Exception ex)
            {
                // Handle exceptions (e.g., file not found, deserialization errors)
                Console.WriteLine($"Error loading project: {ex.Message}");
                return null;
            }
        }

        public void Save(string projectFilePath, Domain.Project.Project project)
        {
            ProjectSerializer.SerializeProject(project, projectFilePath);
        }
    }
}
