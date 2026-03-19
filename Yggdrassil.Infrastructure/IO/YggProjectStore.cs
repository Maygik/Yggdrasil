using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Project;
using Yggdrassil.Infrastructure.Serialization;

namespace Yggdrassil.Infrastructure.IO
{
    public class YggProjectStore : IProjectStore
    {
        public Project LoadProject(string projectFilePath)
        {
            // Open the file and Deserialize using ProjectSerializer.Deserialize
            var project = ProjectSerializer.DeserializeProject(projectFilePath);
            return project;
        }

        public void Save(string projectFilePath, Domain.Project.Project project)
        {
            ProjectSerializer.SerializeProject(project, projectFilePath);
        }
    }
}
