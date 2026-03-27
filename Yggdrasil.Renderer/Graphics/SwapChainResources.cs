using System;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Graphics;

internal sealed class SwapChainResources : IDisposable
{
    public IDXGISwapChain1? SwapChain { get; private set; }

    public ID3D11RenderTargetView? RenderTargetView { get; private set; }

    public ID3D11DepthStencilView? DepthStencilView { get; private set; }

    // Current pixel size of the swap chain
    public Vector2i PixelSize { get; private set; } = Vector2i.Zero;

    // Creates the swap chain and associated render targets
    // Recreates if it already exists
    public void CreateForSurface(DeviceResources deviceResources, Vector2i pixelSize)
    {
        var (device, factory) = GetRequiredDeviceObjects(deviceResources);
        var validatedSize = ValidatePixelSize(pixelSize);

        Dispose();

        var swapChainDesc = CreateSwapChainDescription(validatedSize);
        SwapChain = factory.CreateSwapChainForComposition(device, swapChainDesc);

        CreateTargets(device, validatedSize);
        PixelSize = validatedSize;
    }

    // Resizes the existing swap chain and render tarets rather than creating new ones
    public void Resize(DeviceResources deviceResources, Vector2i pixelSize)
    {
        if (pixelSize.X <= 0 || pixelSize.Y <= 0)
        {
            ReleaseTargets();
            PixelSize = Vector2i.Zero;
            return;
        }

        if (SwapChain == null)
        {
            throw new InvalidOperationException("Cannot resize swap-chain resources before a surface has been created.");
        }

        if (PixelSize == pixelSize && RenderTargetView != null && DepthStencilView != null)
        {
            return;
        }

        var (device, _) = GetRequiredDeviceObjects(deviceResources);

        ReleaseTargets();
        SwapChain.ResizeBuffers(
            0,
            (uint)pixelSize.X,
            (uint)pixelSize.Y,
            Format.Unknown,
            SwapChainFlags.None);

        CreateTargets(device, pixelSize);
        PixelSize = pixelSize;
    }

    // Disposes the render target and depth stencil views but keeps the swap chain intact
    public void ReleaseTargets()
    {
        RenderTargetView?.Dispose();
        RenderTargetView = null;

        DepthStencilView?.Dispose();
        DepthStencilView = null;
    }

    // Disposes all resources including the swap chain
    public void Dispose()
    {
        ReleaseTargets();

        SwapChain?.Dispose();
        SwapChain = null;

        PixelSize = Vector2i.Zero;
    }

    // Creates a swap chain description with the specified pixel size and default settings for other parameters
    private static SwapChainDescription1 CreateSwapChainDescription(Vector2i pixelSize)
    {
        return new SwapChainDescription1
        {
            Width = (uint)pixelSize.X,
            Height = (uint)pixelSize.Y,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Premultiplied
        };
    }

    // Creates the render target view for the back buffer and a depth stencil view with the specified pixel size
    private void CreateTargets(ID3D11Device device, Vector2i pixelSize)
    {
        if (SwapChain == null)
        {
            throw new InvalidOperationException("Cannot create render targets without a swap chain.");
        }

        using var backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = device.CreateRenderTargetView(backBuffer);

        var depthStencilDesc = new Texture2DDescription
        {
            Width = (uint)pixelSize.X,
            Height = (uint)pixelSize.Y,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.D24_UNorm_S8_UInt,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil
        };

        using var depthStencilBuffer = device.CreateTexture2D(depthStencilDesc);
        DepthStencilView = device.CreateDepthStencilView(depthStencilBuffer);
    }

    // Makes sure we've actually got pixels
    private static Vector2i ValidatePixelSize(Vector2i pixelSize)
    {
        if (pixelSize.X <= 0 || pixelSize.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize), "Swap-chain pixel size must be greater than zero.");
        }

        return pixelSize;
    }

    // Retrieves the D3D11 device and DXGI factory from the provided device resources
    private static (ID3D11Device Device, IDXGIFactory2 Factory) GetRequiredDeviceObjects(DeviceResources deviceResources)
    {
        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");
        var factory = deviceResources.Factory
            ?? throw new InvalidOperationException("DXGI factory has not been initialized.");

        return (device, factory);
    }
}
