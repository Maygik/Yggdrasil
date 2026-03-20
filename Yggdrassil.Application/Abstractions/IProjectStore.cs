using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Application.Abstractions
{
    public interface IProjectStore
    {
        Project? LoadProject(string projectFilePath);
        void Save(string projectFilePath, Project project);
    }
}
