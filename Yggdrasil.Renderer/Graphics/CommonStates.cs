using Vortice.Direct3D11;

namespace Yggdrasil.Renderer.Graphics;

internal sealed class CommonStates : IDisposable
{
    public ID3D11DepthStencilState? DepthEnabledState { get; private set; }

    public ID3D11DepthStencilState? DepthDisabledState { get; private set; }

    public ID3D11RasterizerState? SolidRasterizerState { get; private set; }

    public ID3D11RasterizerState? NoCullRasterizerState { get; private set; }

    public ID3D11BlendState? OpaqueBlendState { get; private set; }

    public ID3D11BlendState? AlphaBlendState { get; private set; }

    public ID3D11BlendState? AdditiveBlendState { get; private set; }

    public void Initialize(DeviceResources deviceResources)
    {
        Dispose();

        var depthStencilDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.Less,
            StencilEnable = false
        };

        DepthEnabledState = deviceResources.Device?.CreateDepthStencilState(depthStencilDesc);

        depthStencilDesc.DepthEnable = false;
        DepthDisabledState = deviceResources.Device?.CreateDepthStencilState(depthStencilDesc);

        var rasterizerDesc = new RasterizerDescription
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.Back,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };
        SolidRasterizerState = deviceResources.Device?.CreateRasterizerState(rasterizerDesc);

        rasterizerDesc.CullMode = CullMode.None;
        NoCullRasterizerState = deviceResources.Device?.CreateRasterizerState(rasterizerDesc);

        var blendDesc = new BlendDescription
        {
            AlphaToCoverageEnable = false,
            IndependentBlendEnable = false
        };
        blendDesc.RenderTarget[0].BlendEnable = false;
        blendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;
        OpaqueBlendState = deviceResources.Device?.CreateBlendState(blendDesc);

        blendDesc.RenderTarget[0].BlendEnable = true;
        blendDesc.RenderTarget[0].SourceBlend = Blend.SourceAlpha;
        blendDesc.RenderTarget[0].DestinationBlend = Blend.InverseSourceAlpha;
        blendDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendDesc.RenderTarget[0].SourceBlendAlpha = Blend.One;
        blendDesc.RenderTarget[0].DestinationBlendAlpha = Blend.InverseSourceAlpha;
        blendDesc.RenderTarget[0].BlendOperationAlpha = BlendOperation.Add;
        AlphaBlendState = deviceResources.Device?.CreateBlendState(blendDesc);

        blendDesc.RenderTarget[0].SourceBlend = Blend.SourceAlpha;
        blendDesc.RenderTarget[0].DestinationBlend = Blend.One;
        blendDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendDesc.RenderTarget[0].SourceBlendAlpha = Blend.One;
        blendDesc.RenderTarget[0].DestinationBlendAlpha = Blend.One;
        blendDesc.RenderTarget[0].BlendOperationAlpha = BlendOperation.Add;
        AdditiveBlendState = deviceResources.Device?.CreateBlendState(blendDesc);
    }

    public void Dispose()
    {
        DepthEnabledState?.Dispose();
        DepthEnabledState = null;

        DepthDisabledState?.Dispose();
        DepthDisabledState = null;

        SolidRasterizerState?.Dispose();
        SolidRasterizerState = null;

        NoCullRasterizerState?.Dispose();
        NoCullRasterizerState = null;

        OpaqueBlendState?.Dispose();
        OpaqueBlendState = null;

        AlphaBlendState?.Dispose();
        AlphaBlendState = null;

        AdditiveBlendState?.Dispose();
        AdditiveBlendState = null;
    }
}
