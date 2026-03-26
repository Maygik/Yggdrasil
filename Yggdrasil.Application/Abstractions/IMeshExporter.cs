using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrasil.Application.Abstractions
{
    /// <summary>
    ///  Abstraction for exporting mesh and skeleton data to SMD or DMX formats for use in Source engine tools.
    /// </summary>
    public interface IMeshExporter
    {
        Task ExportSceneAsync(string folderPath, Domain.Scene.SceneModel scene);
        Task ExportAnimationAsync(string folderPath, string animationName, Domain.Scene.SceneModel scene);
    }
}
