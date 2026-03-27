using Vortice.Direct3D11;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class LineListResources : IDisposable
{
    public ID3D11Buffer? VertexBuffer { get; init; }

    public uint VertexCount { get; init; }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
    }
}
