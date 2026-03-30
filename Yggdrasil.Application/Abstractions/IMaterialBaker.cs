using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.Scene;

namespace Yggdrasil.Application.Abstractions
{
    /// <summary>
    /// Abstraction for baking textures into Source-compatible VTF/VMT outputs.
    /// </summary>
    public interface IMaterialBaker
    {
        ServiceResult BakeTextures(MaterialBakeRequest request);
    }

    public sealed class MaterialBakeRequest
    {
        public required IReadOnlyCollection<SourceMaterialSettings> Materials { get; init; }
        public required IReadOnlyDictionary<string, string> UniqueTextureNames { get; init; }
        public required string OutputDirectory { get; init; }
        public required string VtfCmdPath { get; init; }
        public required VtfImageFormat DefaultTextureFormat { get; init; }
        public required VtfImageFormat NormalMapFormat { get; init; }
        public IReadOnlyCollection<string>? UsedInternalTextures { get; init; }
    }
}
