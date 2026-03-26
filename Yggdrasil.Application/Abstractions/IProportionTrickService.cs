using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.Scene;

namespace Yggdrasil.Application.Abstractions
{
    public interface IProportionTrickService
    {
        ProportionTrickResult Build(Project project);
    }

    public sealed class ProportionTrickResult
    {
        public SceneModel Proportions { get; set; } = null!;
        public SceneModel ReferenceMale { get; set; } = null!;
        public SceneModel ReferenceFemale { get; set; } = null!;
    }
}
