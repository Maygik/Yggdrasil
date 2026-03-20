using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Application.Abstractions
{
    public interface IProportionTrickService
    {
        ProportionTrickResult Build(Project project);
    }

    public sealed class ProportionTrickResult
    {
        public SceneModel Proportions { get; set; }
        public SceneModel ReferenceMale { get; set; }
        public SceneModel ReferenceFemale { get; set; }
    }
}
