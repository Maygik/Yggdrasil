using Vortice.Direct3D11;
using Yggdrasil.Renderer.Graphics.Shaders;
using Yggdrasil.Renderer.Scene;

namespace Yggdrasil.Renderer.Graphics.Scene;

internal sealed class MaterialGpuResources : IDisposable
{
    public RenderMaterialSnapshot Snapshot { get; init; } = new();

    public MaterialShaderKey ShaderKey { get; init; }

    public ID3D11ShaderResourceView? BaseTextureView { get; init; }

    public ID3D11ShaderResourceView? NormalTextureView { get; init; }

    public ID3D11Buffer? MaterialConstantsBuffer { get; init; }

    public void Dispose()
    {
        // Texture views are expected to be cache-owned once TextureCache is wired in.
        MaterialConstantsBuffer?.Dispose();
    }
}
