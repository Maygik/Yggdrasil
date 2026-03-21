using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Presentation.ViewModels
{
    public class ProjectSessionViewModel
    {
        public Project? Project;
        public string? ProjectFilePath;
        public string DisplayName => ProjectFilePath != null ? System.IO.Path.GetFileNameWithoutExtension(ProjectFilePath) : "Untitled Project";
        public bool HasScene => Project?.Scene != null;
        public string? RootBoneName => Project?.Scene?.RootBone?.Name;
    }
}
