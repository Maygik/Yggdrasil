using Vortice.Direct3D11;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class MeshGpuResources : IDisposable
{
    public ID3D11Buffer? VertexBuffer { get; init; }

    public ID3D11Buffer? IndexBuffer { get; init; }

    public ID3D11Buffer? SkinningBuffer { get; init; }

    // Only need the index count
    // Index will never go beyond the highest index vertex in VertexBuffer
    public uint IndexCount { get; init; }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
        SkinningBuffer?.Dispose();
    }
}
