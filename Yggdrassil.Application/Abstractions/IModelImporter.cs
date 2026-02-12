using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Application.Abstractions
{
    /// <summary>
    /// Abstraction for importing rigged models into an internal scene representation.
    /// </summary>
    public interface IModelImporter
    {
        Task<SceneModel> ImportModel(string filePath);
    }
}
