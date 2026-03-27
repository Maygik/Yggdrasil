using System.Threading;
using System.Threading.Tasks;
using Vortice.DXGI;

namespace Yggdrasil.Renderer.Api;

public interface IRenderSurfaceHost
{
    ValueTask BindSwapChainAsync(IDXGISwapChain1 swapChain, CancellationToken cancellationToken = default);

    ValueTask ClearSwapChainAsync(CancellationToken cancellationToken = default);
}
