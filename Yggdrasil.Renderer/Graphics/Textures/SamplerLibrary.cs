using Vortice.Direct3D11;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Graphics.Textures;

internal sealed class SamplerLibrary
{
    public ID3D11SamplerState? GetDefaultSampler(DeviceResources deviceResources)
    {
        var samplerDesc = new SamplerDescription
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLODBias = 0.0f,
            MaxAnisotropy = 1,
            ComparisonFunc = ComparisonFunction.Never,
            BorderColor = new Vortice.Mathematics.Color4(0, 0, 0, 0),
            MinLOD = 0,
            MaxLOD = float.MaxValue
        };

        return deviceResources.Device?.CreateSamplerState(samplerDesc);
    }
}
