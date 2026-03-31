using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Yggdrasil.Renderer.Graphics;

internal sealed class DeviceResources
{
    public ID3D11Device? Device { get; private set; }

    public ID3D11DeviceContext? DeviceContext { get; private set; }

    public IDXGIFactory2? Factory { get; private set; }

    // Initializes the D3D11 device, device context, and DXGI factory.
    public void Initialize()
    {
        var creationFlags = DeviceCreationFlags.BgraSupport;
        // Enable debug layer in debug mode
#if DEBUG
        creationFlags |= DeviceCreationFlags.Debug;
#endif
        var featureLevels = new[]
        {
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0,
            FeatureLevel.Level_9_3,
            FeatureLevel.Level_9_2,
            FeatureLevel.Level_9_1
        };
        var result = D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            creationFlags,
            featureLevels,
            out var device,
            out var featureLevel,
            out var deviceContext);
        if (result.Failure)
        {
            throw new InvalidOperationException($"Failed to create D3D11 device: {result}");
        }
        Device = device;
        DeviceContext = deviceContext;
#if DEBUG
        // Enable the debug layer if in debug mode
        Factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(true);
#else
        Factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(false);
#endif


    }
}
