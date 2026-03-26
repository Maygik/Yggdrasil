using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Domain.Scene;

namespace Yggdrasil.Application.Abstractions
{
    /// <summary>
    /// Abstraction for importing rigged models into an internal scene representation.
    /// </summary>
    public interface IModelImporter
    {
        Task<SceneModel> ImportModelAsync(string filePath);
    }
}
