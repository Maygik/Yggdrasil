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
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;
    private ID3D11InputLayout? _inputLayout;
    private ID3D11Buffer? _perFrameBuffer;
    private ID3D11Buffer? _perObjectBuffer;
    private ID3D11SamplerState? _defaultSampler;

    public void EnsureCreated(DeviceResources deviceResources)
    {
        if (_vertexShader != null
            && _pixelShader != null
            && _inputLayout != null
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
            var vertexShaderBytes = GetBlobBytes(vertexShaderBlob);
            var pixelShaderBytes = GetBlobBytes(pixelShaderBlob);

            _vertexShader = device.CreateVertexShader(vertexShaderBytes);
            _pixelShader = device.CreatePixelShader(pixelShaderBytes);
            _inputLayout = device.CreateInputLayout(CreateInputElements(), vertexShaderBytes);

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
        OrbitCameraState cameraState)
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

            stage = "BuildCamera";
            var viewMatrix = OrbitCameraMath.CreateViewMatrix(cameraState);
            var projectionMatrix = OrbitCameraMath.CreateProjectionMatrix(cameraState, (float)pixelSize.X / pixelSize.Y);
            var cameraPosition = OrbitCameraMath.CalculateCameraPosition(cameraState);

            stage = "UploadPerFrame";
            var perFrame = new PerFrameConstants
            {
                ViewProjection = PackedMatrix4x4.FromMatrix(viewMatrix * projectionMatrix),
                CameraPositionX = cameraPosition.X,
                CameraPositionY = cameraPosition.Y,
                CameraPositionZ = cameraPosition.Z,
                Padding0 = 0.0f,
                LightDirectionX = -0.35f,
                LightDirectionY = -0.20f,
                LightDirectionZ = -0.85f,
                AmbientStrength = 0.28f
            };

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
                    World = PackedMatrix4x4.FromMatrix(drawItem.Mesh.ModelMatrix),
                    HighlightMix = CalculateHighlightMix(drawItem.Mesh.MaterialName, selection),
                    IsSelected = IsSelected(drawItem.Mesh.MaterialName, selection) ? 1.0f : 0.0f,
                    IsHovered = IsHovered(drawItem.Mesh.MaterialName, selection) ? 1.0f : 0.0f,
                    Padding = 0.0f
                };

                deviceContext.UpdateSubresource(in perObject, _perObjectBuffer!);
                deviceContext.VSSetConstantBuffers(1, new[] { _perObjectBuffer! });
                deviceContext.PSSetConstantBuffers(1, new[] { _perObjectBuffer! });
                deviceContext.PSSetConstantBuffers(2, new[] { materialResources.MaterialConstantsBuffer });

                ApplyMaterialState(deviceContext, commonStates, materialResources.ShaderKey);

                var stride = (uint)Marshal.SizeOf<ModelVertex>();
                const uint offset = 0;
                deviceContext.IASetVertexBuffers(0, new[] { meshResources.VertexBuffer }, new[] { stride }, new uint[] { offset });
                deviceContext.IASetIndexBuffer(meshResources.IndexBuffer, Format.R32_UInt, 0);
                deviceContext.DrawIndexed(meshResources.IndexCount, 0, 0);
            }
        }
        catch (Exception ex)
        {
            RendererDiagnostics.WriteException($"PreviewShaderFamily.DrawScene stage={stage}", ex);
            throw;
        }
    }

    public void DrawFloor(DeviceResources deviceResources, SwapChainResources swapChainResources, FloorGridResources floorResources)
    {
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

        _pixelShader?.Dispose();
        _pixelShader = null;

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
        if (IsSelected(materialName, selection))
        {
            return 0.8f;
        }

        if (IsHovered(materialName, selection))
        {
            return 0.45f;
        }

        return 0.0f;
    }

    private static bool IsSelected(string materialName, RenderSelectionState selection)
    {
        return !string.IsNullOrWhiteSpace(selection.SelectedMaterialName)
            && string.Equals(materialName, selection.SelectedMaterialName, StringComparison.Ordinal);
    }

    private static bool IsHovered(string materialName, RenderSelectionState selection)
    {
        return !string.IsNullOrWhiteSpace(selection.HoveredMaterialName)
            && string.Equals(materialName, selection.HoveredMaterialName, StringComparison.Ordinal);
    }
}
