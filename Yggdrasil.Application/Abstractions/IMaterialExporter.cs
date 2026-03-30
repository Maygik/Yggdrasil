using System.Collections.Generic;
using Yggdrasil.Domain.Scene;

namespace Yggdrasil.Application.Abstractions
{
    public interface IMaterialExporter
    {
        string GenerateVMT(
            SourceMaterialSettings materialSettings,
            string relativePath,
            Dictionary<string, string> uniqueTextureNames,
            out List<string>? usedInternalTextures);
    }
}
