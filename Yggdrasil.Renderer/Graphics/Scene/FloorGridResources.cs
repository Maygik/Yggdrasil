using System;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class FloorGridResources : IDisposable
{
    public MeshGpuResources? MeshResources { get; init; }

    public void Dispose()
    {
        MeshResources?.Dispose();
    }
}
