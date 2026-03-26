using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Domain.Project;

namespace Yggdrasil.Application.Abstractions
{
    public interface IProjectStore
    {
        Project? LoadProject(string projectFilePath);
        void Save(string projectFilePath, Project project);
    }
}
