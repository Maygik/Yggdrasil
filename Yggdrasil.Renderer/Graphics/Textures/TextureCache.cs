using System;
using System.Collections.Generic;
using System.IO;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.WIC;
using Yggdrasil.Renderer.Runtime;

namespace Yggdrasil.Renderer.Graphics.Textures;

internal sealed class TextureCache : IDisposable
{
    private readonly Dictionary<string, ID3D11ShaderResourceView> _textures = new(StringComparer.OrdinalIgnoreCase);
    private IWICImagingFactory? _imagingFactory;

    public ID3D11ShaderResourceView? GetTexture(DeviceResources deviceResources, string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return null;
        }

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(absolutePath);
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"TextureCache.GetTexture invalid-path '{absolutePath}'", ex);
            return null;
        }

        if (_textures.TryGetValue(normalizedPath, out var cachedTexture))
        {
            return cachedTexture;
        }

        if (!File.Exists(normalizedPath))
        {
            RendererDiagnostics.Write($"TextureCache.GetTexture: file not found '{normalizedPath}'.");
            return null;
        }

        try
        {
            var texture = LoadTexture(deviceResources, normalizedPath);
            if (texture != null)
            {
                _textures[normalizedPath] = texture;
            }

            return texture;
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"TextureCache.GetTexture failed '{normalizedPath}'", ex);
            return null;
        }
    }

    public void Clear()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
        _imagingFactory?.Dispose();
        _imagingFactory = null;
    }

    public void Dispose()
    {
        Clear();
    }

    private unsafe ID3D11ShaderResourceView? LoadTexture(DeviceResources deviceResources, string absolutePath)
    {
        var device = deviceResources.Device
            ?? throw new InvalidOperationException("D3D11 device has not been initialized.");
        var deviceContext = deviceResources.DeviceContext
            ?? throw new InvalidOperationException("D3D11 device context has not been initialized.");

        using var fileHandle = File.OpenHandle(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var nativeHandle = new UIntPtr(unchecked((ulong)fileHandle.DangerousGetHandle().ToInt64()));
        using var decoder = GetImagingFactory().CreateDecoderFromFileHandle(
            nativeHandle,
            null,
            DecodeOptions.CacheOnLoad);
        using var frame = decoder.GetFrame(0);
        using var formatConverter = GetImagingFactory().CreateFormatConverter();

        formatConverter.Initialize(frame, PixelFormat.Format32bppRGBA);
        formatConverter.GetSize(out var width, out var height);

        if (width == 0 || height == 0)
        {
            return null;
        }

        var rowPitch = checked((uint)(width * 4));
        var slicePitch = checked(rowPitch * height);
        var pixelBytes = new byte[checked((int)slicePitch)];
        var sourceRect = new Vortice.Mathematics.RectI(0, 0, checked((int)width), checked((int)height));

        fixed (byte* pixelBytePtr = pixelBytes)
        {
            formatConverter.CopyPixels(sourceRect, rowPitch, slicePitch, (nint)pixelBytePtr);

            var textureDescription = new Texture2DDescription(
                Format.R8G8B8A8_UNorm,
                width,
                height,
                1,
                1,
                BindFlags.ShaderResource,
                ResourceUsage.Default,
                CpuAccessFlags.None,
                1,
                0,
                ResourceOptionFlags.None);

            using var texture = device.CreateTexture2D(textureDescription);
            deviceContext.UpdateSubresource(texture, 0, null, (IntPtr)pixelBytePtr, rowPitch, slicePitch);

            return device.CreateShaderResourceView(texture);
        }
    }

    private IWICImagingFactory GetImagingFactory()
    {
        _imagingFactory ??= new IWICImagingFactory();
        return _imagingFactory;
    }
}
