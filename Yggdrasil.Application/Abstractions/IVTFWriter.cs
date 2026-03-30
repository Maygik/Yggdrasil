using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;

namespace Yggdrasil.Application.Abstractions
{
    public interface IVTFWriter
    {
        ServiceResult WriteVtf(VtfWriteRequest request);
    }

    public sealed class VtfWriteRequest
    {
        public required string ToolPath { get; init; }
        public required string SourceImagePath { get; init; }
        public required string OutputDirectory { get; init; }
        public required VtfImageFormat Format { get; init; }
        public bool MarkAsNormalMap { get; init; }
    }
}
