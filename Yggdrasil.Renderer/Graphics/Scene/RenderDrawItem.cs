using Yggdrasil.Renderer.Scene;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class RenderDrawItem
{
    public RenderMeshSnapshot Mesh { get; init; } = new();

    public MeshGpuResources? MeshResources { get; init; }

    public MaterialGpuResources? MaterialResources { get; init; }
}
