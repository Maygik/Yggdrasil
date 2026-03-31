using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Yggdrasil.Renderer.Camera;
using Yggdrasil.Renderer.Graphics.Buffers;
using Yggdrasil.Renderer.Graphics.Scene;
using Yggdrasil.Renderer.Graphics.Textures;
using Yggdrasil.Renderer.Runtime;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Graphics.Shaders;

internal sealed class PreviewShaderFamily : IDisposable
{
    private readonly SamplerLibrary _samplerLibrary = new();
    private static readonly ID3D11ShaderResourceView[] EmptyMaterialTextures = new ID3D11ShaderResourceView[8];
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;
    private ID3D11VertexShader? _floorVertexShader;
    private ID3D11PixelShader? _floorPixelShader;
    private ID3D11PixelShader? _heightPlanePixelShader;
    private ID3D11VertexShader? _debugLineVertexShader;
    private ID3D11PixelShader? _debugLinePixelShader;
    private ID3D11InputLayout? _inputLayout;
    private ID3D11InputLayout? _debugLineInputLayout;
    private ID3D11Buffer? _perFrameBuffer;
    private ID3D11Buffer? _perObjectBuffer;
    private ID3D11SamplerState? _defaultSampler;

    public void EnsureCreated(DeviceResources deviceResources)
    {
        if (_vertexShader != null
            && _pixelShader != null
            && _floorVertexShader != null
            && _floorPixelShader != null
            && _heightPlanePixelShader != null
            && _debugLineVertexShader != null
            && _debugLinePixelShader != null
            && _inputLayout != null
            && _debugLineInputLayout != null
            && _perFrameBuffer != null
            && _perObjectBuffer != null
            && _defaultSampler != null)
        {
            return;
        }

        try
        {
            var device = deviceResources.Device
                ?? throw new InvalidOperationException("D3D11 device has not been initialized.");

            using var vertexShaderBlob = CompileShader("PreviewLit.hlsl", "VSMain", "vs_5_0");
            using var pixelShaderBlob = CompileShader("PreviewLit.hlsl", "PSMain", "ps_5_0");
            using var floorVertexShaderBlob = CompileShader("FloorGrid.hlsl", "VSMain", "vs_5_0");
            using var floorPixelShaderBlob = CompileShader("FloorGrid.hlsl", "PSMain", "ps_5_0");
            using var heightPlanePixelShaderBlob = CompileShader("HeightPlane.hlsl", "PSMain", "ps_5_0");
            using var debugLineVertexShaderBlob = CompileShader("DebugLines.hlsl", "VSMain", "vs_5_0");
            using var debugLinePixelShaderBlob = CompileShader("DebugLines.hlsl", "PSMain", "ps_5_0");
            var vertexShaderBytes = GetBlobBytes(vertexShaderBlob);
            var pixelShaderBytes = GetBlobBytes(pixelShaderBlob);
            var floorVertexShaderBytes = GetBlobBytes(floorVertexShaderBlob);
            var floorPixelShaderBytes = GetBlobBytes(floorPixelShaderBlob);
            var heightPlanePixelShaderBytes = GetBlobBytes(heightPlanePixelShaderBlob);
            var debugLineVertexShaderBytes = GetBlobBytes(debugLineVertexShaderBlob);
            var debugLinePixelShaderBytes = GetBlobBytes(debugLinePixelShaderBlob);

            _vertexShader = device.CreateVertexShader(vertexShaderBytes);
            _pixelShader = device.CreatePixelShader(pixelShaderBytes);
            _floorVertexShader = device.CreateVertexShader(floorVertexShaderBytes);
            _floorPixelShader = device.CreatePixelShader(floorPixelShaderBytes);
            _heightPlanePixelShader = device.CreatePixelShader(heightPlanePixelShaderBytes);
            _debugLineVertexShader = device.CreateVertexShader(debugLineVertexShaderBytes);
            _debugLinePixelShader = device.CreatePixelShader(debugLinePixelShaderBytes);
            _inputLayout = device.CreateInputLayout(CreateInputElements(), vertexShaderBytes);
            _debugLineInputLayout = device.CreateInputLayout(CreateDebugLineInputElements(), debugLineVertexShaderBytes);

            _perFrameBuffer = device.CreateBuffer(
                (uint)Marshal.SizeOf<PerFrameConstants>(),
                BindFlags.ConstantBuffer,
                ResourceUsage.Default,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            _perObjectBuffer = device.CreateBuffer(
                (uint)Marshal.SizeOf<PerObjectConstants>(),
                BindFlags.ConstantBuffer,
                ResourceUsage.Default,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            _defaultSampler = _samplerLibrary.GetDefaultSampler(deviceResources);
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException("PreviewShaderFamily.EnsureCreated", ex);
            throw;
        }
    }

    public void DrawScene(
        DeviceResources deviceResources,
        SwapChainResources swapChainResources,
        CommonStates commonStates,
        IReadOnlyList<RenderDrawItem> drawItems,
        RenderSelectionState selection,
        OrbitCameraState cameraState,
        OrbitLightState lightState,
        Matrix4x4 modelTransform)
    {
        if (drawItems.Count == 0)
        {
            return;
        }

        var stage = "Initialize";

        try
        {
            EnsureCreated(deviceResources);

            stage = "AcquireContext";
            var deviceContext = deviceResources.DeviceContext
                ?? throw new InvalidOperationException("D3D11 device context has not been initialized.");
            var pixelSize = swapChainResources.PixelSize;

            if (pixelSize.X <= 0 || pixelSize.Y <= 0)
            {
                return;
            }

            stage = "UploadPerFrame";
            var perFrame = CreatePerFrameConstants(cameraState, pixelSize, lightState);

            deviceContext.UpdateSubresource(in perFrame, _perFrameBuffer!);
            deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            deviceContext.IASetInputLayout(_inputLayout);
            deviceContext.VSSetShader(_vertexShader);
            deviceContext.PSSetShader(_pixelShader);
            deviceContext.VSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.PSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.VSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            if (_defaultSampler != null)
            {
                deviceContext.PSSetSamplers(0, new[] { _defaultSampler });
            }

            for (var i = 0; i < drawItems.Count; i++)
            {
                var drawItem = drawItems[i];
                stage = $"DrawItem[{i}] {drawItem.Mesh.Name}";

                var meshResources = drawItem.MeshResources;
                var materialResources = drawItem.MaterialResources;
                if (meshResources?.VertexBuffer == null
                    || meshResources.IndexBuffer == null
                    || meshResources.IndexCount == 0
                    || materialResources?.MaterialConstantsBuffer == null)
                {
                    RendererDiagnostics.Write($"{stage}: skipped because required GPU resources were missing.");
                    continue;
                }

                var perObject = new PerObjectConstants
                {
                    World = PackedMatrix4x4.FromMatrix(modelTransform),
                    HighlightMix = CalculateHighlightMix(drawItem.Mesh.MaterialName, selection),
                    IsHovered = IsHovered(drawItem.Mesh.MaterialName, selection) ? 1.0f : 0.0f
                };

                deviceContext.UpdateSubresource(in perObject, _perObjectBuffer!);
                deviceContext.VSSetConstantBuffers(1, new[] { _perObjectBuffer! });
                deviceContext.PSSetConstantBuffers(1, new[] { _perObjectBuffer! });
                deviceContext.VSSetConstantBuffers(2, new[] { materialResources.MaterialConstantsBuffer });
                deviceContext.PSSetConstantBuffers(2, new[] { materialResources.MaterialConstantsBuffer });
                deviceContext.PSSetShaderResources(
                    0,
                    materialResources.TextureViews.Length == EmptyMaterialTextures.Length
                        ? materialResources.TextureViews
                        : EmptyMaterialTextures);

                ApplyMaterialState(deviceContext, commonStates, materialResources.ShaderKey);

                var stride = (uint)Marshal.SizeOf<ModelVertex>();
                const uint offset = 0;
                deviceContext.IASetVertexBuffers(0, new[] { meshResources.VertexBuffer }, new[] { stride }, new uint[] { offset });
                deviceContext.IASetIndexBuffer(meshResources.IndexBuffer, Format.R32_UInt, 0);
                deviceContext.DrawIndexed(meshResources.IndexCount, 0, 0);
            }

            deviceContext.PSSetShaderResources(0, EmptyMaterialTextures);
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"PreviewShaderFamily.DrawScene stage={stage}", ex);
            throw;
        }
    }

    public void DrawFloor(
        DeviceResources deviceResources,
        SwapChainResources swapChainResources,
        CommonStates commonStates,
        FloorGridResources floorResources,
        OrbitCameraState cameraState,
        OrbitLightState lightState)
    {
        var meshResources = floorResources.MeshResources;
        if (meshResources?.VertexBuffer == null
            || meshResources.IndexBuffer == null
            || meshResources.IndexCount == 0)
        {
            return;
        }

        var stage = "Initialize";

        try
        {
            EnsureCreated(deviceResources);

            stage = "AcquireContext";
            var deviceContext = deviceResources.DeviceContext
                ?? throw new InvalidOperationException("D3D11 device context has not been initialized.");
            var pixelSize = swapChainResources.PixelSize;

            if (pixelSize.X <= 0 || pixelSize.Y <= 0)
            {
                return;
            }

            stage = "UploadPerFrame";
            var perFrame = CreatePerFrameConstants(cameraState, pixelSize, lightState);
            var perObject = CreatePerObjectConstants(Matrix4x4.Identity);
            deviceContext.UpdateSubresource(in perFrame, _perFrameBuffer!);
            deviceContext.UpdateSubresource(in perObject, _perObjectBuffer!);

            deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            deviceContext.IASetInputLayout(_inputLayout);
            deviceContext.VSSetShader(_floorVertexShader);
            deviceContext.PSSetShader(_floorPixelShader);
            deviceContext.VSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.PSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.VSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            deviceContext.PSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            deviceContext.OMSetBlendState(commonStates.OpaqueBlendState);
            deviceContext.OMSetDepthStencilState(commonStates.DepthEnabledState);
            deviceContext.RSSetState(commonStates.NoCullRasterizerState);

            var stride = (uint)Marshal.SizeOf<ModelVertex>();
            const uint offset = 0;
            deviceContext.IASetVertexBuffers(0, new[] { meshResources.VertexBuffer }, new[] { stride }, new uint[] { offset });
            deviceContext.IASetIndexBuffer(meshResources.IndexBuffer, Format.R32_UInt, 0);
            deviceContext.DrawIndexed(meshResources.IndexCount, 0, 0);
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"PreviewShaderFamily.DrawFloor stage={stage}", ex);
            throw;
        }
    }

    public void DrawHeightPlane(
        DeviceResources deviceResources,
        SwapChainResources swapChainResources,
        CommonStates commonStates,
        FloorGridResources planeResources,
        OrbitCameraState cameraState,
        OrbitLightState lightState,
        Matrix4x4 worldMatrix)
    {
        var meshResources = planeResources.MeshResources;
        if (meshResources?.VertexBuffer == null
            || meshResources.IndexBuffer == null
            || meshResources.IndexCount == 0)
        {
            return;
        }

        var stage = "Initialize";

        try
        {
            EnsureCreated(deviceResources);

            stage = "AcquireContext";
            var deviceContext = deviceResources.DeviceContext
                ?? throw new InvalidOperationException("D3D11 device context has not been initialized.");
            var pixelSize = swapChainResources.PixelSize;

            if (pixelSize.X <= 0 || pixelSize.Y <= 0)
            {
                return;
            }

            stage = "UploadPerFrame";
            var perFrame = CreatePerFrameConstants(cameraState, pixelSize, lightState);
            var perObject = CreatePerObjectConstants(worldMatrix);
            deviceContext.UpdateSubresource(in perFrame, _perFrameBuffer!);
            deviceContext.UpdateSubresource(in perObject, _perObjectBuffer!);

            deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            deviceContext.IASetInputLayout(_inputLayout);
            deviceContext.VSSetShader(_floorVertexShader);
            deviceContext.PSSetShader(_heightPlanePixelShader);
            deviceContext.VSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.PSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.VSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            deviceContext.PSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            deviceContext.OMSetBlendState(commonStates.AlphaBlendState);
            deviceContext.OMSetDepthStencilState(commonStates.DepthEnabledState);
            deviceContext.RSSetState(commonStates.NoCullRasterizerState);

            var stride = (uint)Marshal.SizeOf<ModelVertex>();
            const uint offset = 0;
            deviceContext.IASetVertexBuffers(0, new[] { meshResources.VertexBuffer }, new[] { stride }, new uint[] { offset });
            deviceContext.IASetIndexBuffer(meshResources.IndexBuffer, Format.R32_UInt, 0);
            deviceContext.DrawIndexed(meshResources.IndexCount, 0, 0);
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"PreviewShaderFamily.DrawHeightPlane stage={stage}", ex);
            throw;
        }
    }

    public void DrawLines(
        DeviceResources deviceResources,
        SwapChainResources swapChainResources,
        CommonStates commonStates,
        LineListResources lineResources,
        OrbitCameraState cameraState,
        OrbitLightState lightState,
        Matrix4x4 worldMatrix)
    {
        if (lineResources.VertexBuffer == null || lineResources.VertexCount == 0)
        {
            return;
        }

        var stage = "Initialize";

        try
        {
            EnsureCreated(deviceResources);

            stage = "AcquireContext";
            var deviceContext = deviceResources.DeviceContext
                ?? throw new InvalidOperationException("D3D11 device context has not been initialized.");
            var pixelSize = swapChainResources.PixelSize;

            if (pixelSize.X <= 0 || pixelSize.Y <= 0)
            {
                return;
            }

            stage = "UploadPerFrame";
            var perFrame = CreatePerFrameConstants(cameraState, pixelSize, lightState);
            var perObject = CreatePerObjectConstants(worldMatrix);
            deviceContext.UpdateSubresource(in perFrame, _perFrameBuffer!);
            deviceContext.UpdateSubresource(in perObject, _perObjectBuffer!);

            deviceContext.IASetPrimitiveTopology(PrimitiveTopology.LineList);
            deviceContext.IASetInputLayout(_debugLineInputLayout);
            deviceContext.VSSetShader(_debugLineVertexShader);
            deviceContext.PSSetShader(_debugLinePixelShader);
            deviceContext.VSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.PSSetConstantBuffers(0, new[] { _perFrameBuffer! });
            deviceContext.VSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            deviceContext.PSSetConstantBuffers(1, new[] { _perObjectBuffer! });
            deviceContext.OMSetBlendState(commonStates.OpaqueBlendState);
            deviceContext.OMSetDepthStencilState(commonStates.DepthDisabledState);
            deviceContext.RSSetState(commonStates.NoCullRasterizerState);

            var stride = (uint)Marshal.SizeOf<LineVertex>();
            const uint offset = 0;
            deviceContext.IASetVertexBuffers(0, new[] { lineResources.VertexBuffer }, new[] { stride }, new uint[] { offset });
            deviceContext.Draw(lineResources.VertexCount, 0);
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"PreviewShaderFamily.DrawLines stage={stage}", ex);
            throw;
        }
    }

    public void Dispose()
    {
        _defaultSampler?.Dispose();
        _defaultSampler = null;

        _perObjectBuffer?.Dispose();
        _perObjectBuffer = null;

        _perFrameBuffer?.Dispose();
        _perFrameBuffer = null;

        _inputLayout?.Dispose();
        _inputLayout = null;

        _debugLineInputLayout?.Dispose();
        _debugLineInputLayout = null;

        _debugLinePixelShader?.Dispose();
        _debugLinePixelShader = null;

        _debugLineVertexShader?.Dispose();
        _debugLineVertexShader = null;

        _pixelShader?.Dispose();
        _pixelShader = null;

        _heightPlanePixelShader?.Dispose();
        _heightPlanePixelShader = null;

        _floorPixelShader?.Dispose();
        _floorPixelShader = null;

        _floorVertexShader?.Dispose();
        _floorVertexShader = null;

        _vertexShader?.Dispose();
        _vertexShader = null;
    }

    private static InputElementDescription[] CreateInputElements()
    {
        return
        [
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElementDescription("TANGENT", 0, Format.R32G32B32_Float, 24, 0),
            new InputElementDescription("BINORMAL", 0, Format.R32G32B32_Float, 36, 0),
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 48, 0),
            new InputElementDescription("BLENDINDICES", 0, Format.R32G32B32A32_SInt, 56, 0),
            new InputElementDescription("BLENDWEIGHT", 0, Format.R32G32B32A32_Float, 72, 0)
        ];
    }

    private static InputElementDescription[] CreateDebugLineInputElements()
    {
        return
        [
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
        ];
    }

    private static Vortice.Direct3D.Blob CompileShader(string shaderFileName, string entryPoint, string target)
    {
        var shaderPath = ResolveShaderPath(shaderFileName);
        var flags = ShaderFlags.EnableStrictness;
#if DEBUG
        flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#else
        flags |= ShaderFlags.OptimizationLevel3;
#endif

        var result = Compiler.CompileFromFile(
            shaderPath,
            null,
            null,
            entryPoint,
            target,
            flags,
            EffectFlags.None,
            out var shaderBlob,
            out var errorBlob);

        if (result.Failure)
        {
            var errors = errorBlob?.AsString() ?? result.Description ?? "Unknown shader compilation failure.";
            errorBlob?.Dispose();
            shaderBlob?.Dispose();
            throw new InvalidOperationException($"Failed to compile shader '{Path.GetFileName(shaderPath)}' ({entryPoint}/{target}): {errors}");
        }

        errorBlob?.Dispose();
        return shaderBlob ?? throw new InvalidOperationException($"Shader compilation for '{Path.GetFileName(shaderPath)}' returned no shader blob.");
    }

    private static string ResolveShaderPath(string shaderFileName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var outputPath = Path.Combine(baseDirectory, "Graphics", "Shaders", shaderFileName);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var flatOutputPath = Path.Combine(baseDirectory, shaderFileName);
        if (File.Exists(flatOutputPath))
        {
            return flatOutputPath;
        }

            throw new FileNotFoundException($"Could not locate shader source '{shaderFileName}' in the application output.", shaderFileName);
    }

    private static byte[] GetBlobBytes(Vortice.Direct3D.Blob blob)
    {
        var byteCount = Convert.ToInt32(blob.BufferSize);
        var bytes = new byte[byteCount];
        Marshal.Copy(blob.BufferPointer, bytes, 0, byteCount);
        return bytes;
    }

    private static PerFrameConstants CreatePerFrameConstants(
        OrbitCameraState cameraState,
        Vector2i pixelSize,
        OrbitLightState lightState)
    {
        var viewMatrix = OrbitCameraMath.CreateViewMatrix(cameraState);
        var projectionMatrix = OrbitCameraMath.CreateProjectionMatrix(cameraState, (float)pixelSize.X / pixelSize.Y);
        var cameraPosition = OrbitCameraMath.CalculateCameraPosition(cameraState);
        var lightDirection = OrbitRotationMath.CalculateDirection(lightState.YawRadians, lightState.PitchRadians);

        return new PerFrameConstants
        {
            ViewProjection = PackedMatrix4x4.FromMatrix(viewMatrix * projectionMatrix),
            CameraPositionX = cameraPosition.X,
            CameraPositionY = cameraPosition.Y,
            CameraPositionZ = cameraPosition.Z,
            Padding0 = 0.0f,
            LightDirectionX = lightDirection.X,
            LightDirectionY = lightDirection.Y,
            LightDirectionZ = lightDirection.Z,
            AmbientStrength = Math.Clamp(lightState.AmbientStrength, 0.0f, 1.0f)
        };
    }

    private static PerObjectConstants CreatePerObjectConstants(Matrix4x4 worldMatrix)
    {
        return new PerObjectConstants
        {
            World = PackedMatrix4x4.FromMatrix(worldMatrix),
            HighlightMix = 0.0f,
            IsHovered = 0.0f
        };
    }

    private static void ApplyMaterialState(
        ID3D11DeviceContext deviceContext,
        CommonStates commonStates,
        MaterialShaderKey shaderKey)
    {
        deviceContext.OMSetBlendState(
            shaderKey.RenderMode switch
            {
                MaterialRenderMode.Translucent => commonStates.AlphaBlendState,
                MaterialRenderMode.Additive => commonStates.AdditiveBlendState,
                _ => commonStates.OpaqueBlendState
            });

        deviceContext.OMSetDepthStencilState(commonStates.DepthEnabledState);
        deviceContext.RSSetState(
            shaderKey.Features.HasFlag(VertexLitGenericFeatures.DoubleSided)
                ? commonStates.NoCullRasterizerState
                : commonStates.SolidRasterizerState);
    }

    private static float CalculateHighlightMix(string materialName, RenderSelectionState selection)
    {
        if (IsHovered(materialName, selection))
        {
            return 0.45f;
        }

        return 0.0f;
    }

    private static bool IsHovered(string materialName, RenderSelectionState selection)
    {
        return !string.IsNullOrWhiteSpace(selection.HoveredMaterialName)
            && string.Equals(materialName, selection.HoveredMaterialName, StringComparison.Ordinal);
    }
}
