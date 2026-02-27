using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Infrastructure.Export
{
    /// <summary>
    /// Writes source DMX files from normalized mesh, skeleton and animation data.
    /// Writes to ascii DMX format, which may need to be converted to binary DMX for use in Source engine tools.
    /// </summary>
    internal class DmxExporter : IMeshExporter
    {
        public DmxExporter()
        {
            // Constructor can be used for any necessary initialization, if needed.
        }
        public Task ExportSceneAsync(string folderPath, Domain.Scene.SceneModel scene) => Task.FromResult(ExportScene(folderPath, scene));

        public bool ExportScene(string folderPath, Domain.Scene.SceneModel scene)
        {
            throw new NotImplementedException("DmxExporter.ExportScene is not implemented yet.");
        }

        public Task ExportAnimationAsync(string folderPath, string animationName, SceneModel scene)
        {
            throw new NotImplementedException();
        }
    }
}
