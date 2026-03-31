using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Color4 = Vortice.Mathematics.Color4;
using Viewport = Vortice.Mathematics.Viewport;
using Yggdrasil.Renderer.Api;
using Yggdrasil.Renderer.Graphics;
using Yggdrasil.Renderer.Graphics.Scene;
using Yggdrasil.Renderer.Graphics.Shaders;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Runtime;

/// <summary>
/// The RenderThreadWorker is responsible for processing renderer commands, managing rendering state, and executing the render loop on a dedicated thread.
/// It interacts with the renderer host through a mailbox system to receive commands and updates, and it manages Direct3D resources for rendering the scene.
/// The worker ensures that rendering operations are performed efficiently and that resources are properly cleaned up when no longer needed.
/// </summary>
internal sealed class RenderThreadWorker
{
    private readonly RendererMailbox _mailbox;
    private readonly RendererState _state = new();
    private readonly DeviceResources _deviceResources = new();
    private readonly SwapChainResources _swapChainResources = new();
    private readonly CommonStates _commonStates = new();
    private readonly SceneGpuCache _sceneGpuCache = new();
    private readonly ShaderLibrary _shaderLibrary = new();
    private int _stopRequested;

    public RenderThreadWorker(RendererMailbox mailbox)
    {
        _mailbox = mailbox ?? throw new ArgumentNullException(nameof(mailbox));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        RendererDiagnostics.Reset();
        _deviceResources.Initialize();
        _commonStates.Initialize(_deviceResources);

        try
        {
            while (true)
            {
                RendererCommand command;

                try
                {
                    command = await _mailbox.DequeueAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var shouldStop = await ProcessPendingCommandsAsync(command, cancellationToken).ConfigureAwait(false);
                if (shouldStop)
                {
                    break;
                }

                RenderFrameIfPossible();
            }
        }
        finally
        {
            await CleanupAsync().ConfigureAwait(false);
        }
    }

    // Stop processing commands and exit the loop
    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            return;
        }

        _mailbox.Enqueue(new ShutdownCommand());
    }

    // Returns true if the worker should stop after processing the command
    private async ValueTask<bool> ProcessCommandAsync(RendererCommand command, CancellationToken cancellationToken)
    {
        switch (command)
        {
            case AttachSurfaceCommand attachSurface:
                await AttachSurfaceAsync(attachSurface.SurfaceHost, cancellationToken).ConfigureAwait(false);
                return false;

            case DetachSurfaceCommand:
                await DetachSurfaceAsync(cancellationToken).ConfigureAwait(false);
                return false;

            case ResizeCommand resize:
                await ResizeAsync(resize.PixelSize, cancellationToken).ConfigureAwait(false);
                return false;

            case LoadSceneCommand loadScene:
                _state.Scene = loadScene.Scene;
                _state.CameraState = loadScene.Scene == null
                    ? OrbitCameraState.Default
                    : Camera.OrbitCameraMath.CreateFramedState(loadScene.Scene.Bounds);
                _sceneGpuCache.LoadScene(_deviceResources, loadScene.Scene);
                _state.GeometryDirty = true;
                _state.MaterialsDirty = true;
                return false;

            case UpdateMaterialCommand updateMaterial:
                ApplyMaterialUpdate(updateMaterial.Material);
                return false;

            case SetSelectionCommand setSelection:
                _state.Selection = setSelection.Selection;
                _state.MaterialsDirty = true;
                return false;

            case SetCameraStateCommand setCameraState:
                _state.CameraState = setCameraState.CameraState;
                _state.IsInteracting = setCameraState.IsInteracting;
                return false;

            case SetLightStateCommand setLightState:
                _state.LightState = setLightState.LightState;
                _state.IsInteracting = setLightState.IsInteracting;
                return false;

            case SetViewportOptionsCommand setViewportOptions:
                _state.ViewportOptions = setViewportOptions.ViewportOptions;
                return false;

            case ShutdownCommand:
                return true;

            default:
                throw new InvalidOperationException($"Unsupported renderer command type '{command.GetType().Name}'.");
        }
    }

    private async ValueTask<bool> ProcessPendingCommandsAsync(RendererCommand initialCommand, CancellationToken cancellationToken)
    {
        var shouldStop = await ProcessCommandAsync(initialCommand, cancellationToken).ConfigureAwait(false);

        while (!shouldStop && _mailbox.TryDequeue(out var pendingCommand) && pendingCommand != null)
        {
            shouldStop = await ProcessCommandAsync(pendingCommand, cancellationToken).ConfigureAwait(false);
        }

        return shouldStop;
    }

    // Attaches a new surface host, creating or resizing swap chain resources as needed. If a different surface host is already attached, it will be cleared first.
    private async ValueTask AttachSurfaceAsync(IRenderSurfaceHost surfaceHost, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(surfaceHost);

        if (!ReferenceEquals(_state.SurfaceHost, surfaceHost) && _state.SurfaceHost != null)
        {
            await _state.SurfaceHost.ClearSwapChainAsync(cancellationToken).ConfigureAwait(false);
        }

        _state.SurfaceHost = surfaceHost;
        _state.SurfaceDirty = true;

        if (_state.PixelSize.X <= 0 || _state.PixelSize.Y <= 0)
        {
            return;
        }

        if (_swapChainResources.SwapChain == null)
        {
            _swapChainResources.CreateForSurface(_deviceResources, _state.PixelSize);
        }
        else if (_swapChainResources.PixelSize != _state.PixelSize)
        {
            _swapChainResources.Resize(_deviceResources, _state.PixelSize);
        }

        await surfaceHost.BindSwapChainAsync(_swapChainResources.SwapChain!, cancellationToken).ConfigureAwait(false);
        _state.SurfaceDirty = false;
    }

    // Detachest the current surface host and clears resources.
    private async ValueTask DetachSurfaceAsync(CancellationToken cancellationToken)
    {
        if (_state.SurfaceHost != null)
        {
            await _state.SurfaceHost.ClearSwapChainAsync(cancellationToken).ConfigureAwait(false);
        }

        _state.SurfaceHost = null;
        _state.SurfaceDirty = false;
        _swapChainResources.Dispose();
    }

    // Resizes the swap chain resources to match the new pixel size. 
    private async ValueTask ResizeAsync(Vector2i pixelSize, CancellationToken cancellationToken)
    {
        _state.PixelSize = pixelSize;
        _state.SurfaceDirty = true;

        if (_state.SurfaceHost == null)
        {
            return;
        }

        if (_swapChainResources.SwapChain == null)
        {
            if (pixelSize.X <= 0 || pixelSize.Y <= 0)
            {
                return;
            }

            _swapChainResources.CreateForSurface(_deviceResources, pixelSize);
            await _state.SurfaceHost.BindSwapChainAsync(_swapChainResources.SwapChain!, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _swapChainResources.Resize(_deviceResources, pixelSize);
            await _state.SurfaceHost.BindSwapChainAsync(_swapChainResources.SwapChain!, cancellationToken).ConfigureAwait(false);
        }

        _state.SurfaceDirty = false;
    }

    // Marks materials as dirty to trigger an update on the next render.
    private void ApplyMaterialUpdate(RenderMaterialSnapshot material)
    {
        ArgumentNullException.ThrowIfNull(material);

        _sceneGpuCache.UpdateMaterial(_deviceResources, material);
        _state.MaterialsDirty = true;
    }

    // Clears swap chain and other resources.
    private async ValueTask CleanupAsync()
    {
        if (_state.SurfaceHost != null)
        {
            try
            {
                await _state.SurfaceHost.ClearSwapChainAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            _state.SurfaceHost = null;
        }

        _swapChainResources.Dispose();
        _sceneGpuCache.Dispose();
        _commonStates.Dispose();
        _shaderLibrary.GetPreviewShaderFamily().Dispose();
    }

    // Renders a frame if all necessary resources and state are available. This includes setting render targets, clearing views, and drawing the scene using the preview shader.
    private void RenderFrameIfPossible()
    {
        var deviceContext = _deviceResources.DeviceContext;
        var renderTargetView = _swapChainResources.RenderTargetView;
        var depthStencilView = _swapChainResources.DepthStencilView;
        var swapChain = _swapChainResources.SwapChain;
        var pixelSize = _swapChainResources.PixelSize;

        if (_state.SurfaceHost == null
            || deviceContext == null
            || renderTargetView == null
            || depthStencilView == null
            || swapChain == null
            || pixelSize.X <= 0
            || pixelSize.Y <= 0)
        {
            return;
        }

        deviceContext.OMSetRenderTargets(renderTargetView, depthStencilView);
        deviceContext.RSSetViewport(new Viewport(0, 0, pixelSize.X, pixelSize.Y, 0.0f, 1.0f));

        var clearColor = _state.Scene == null
            ? new Color4(0.09f, 0.10f, 0.12f, 1.0f)
            : new Color4(0.13f, 0.15f, 0.18f, 1.0f);

        deviceContext.ClearRenderTargetView(renderTargetView, clearColor);
        deviceContext.ClearDepthStencilView(
            depthStencilView,
            DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
            1.0f,
            0);

        if (_state.Scene != null)
        {
            try
            {
                var previewShader = _shaderLibrary.GetPreviewShaderFamily();
                var modelTransform = ViewportAlignmentMath.CreateModelTransform(_state.ViewportOptions);
                var heightPlaneTransform = ViewportAlignmentMath.CreateHeightPlaneTransform(_state.ViewportOptions);

                if (_state.ViewportOptions.ShowFloor && _sceneGpuCache.FloorResources != null)
                {
                    previewShader.DrawFloor(
                        _deviceResources,
                        _swapChainResources,
                        _commonStates,
                        _sceneGpuCache.FloorResources,
                        _state.CameraState,
                        _state.LightState);
                }

                if (_sceneGpuCache.DrawItems.Count > 0)
                {
                    previewShader.DrawScene(
                        _deviceResources,
                        _swapChainResources,
                        _commonStates,
                        _sceneGpuCache.DrawItems,
                        _state.Selection,
                        _state.CameraState,
                        _state.LightState,
                        modelTransform);
                }

                if (_state.ViewportOptions.ShowHeightPlane && _sceneGpuCache.HeightPlaneResources != null)
                {
                    previewShader.DrawHeightPlane(
                        _deviceResources,
                        _swapChainResources,
                        _commonStates,
                        _sceneGpuCache.HeightPlaneResources,
                        _state.CameraState,
                        _state.LightState,
                        heightPlaneTransform);
                }

                if (_state.ViewportOptions.ShowBoneConnections && _sceneGpuCache.BoneConnectionResources != null)
                {
                    previewShader.DrawLines(
                        _deviceResources,
                        _swapChainResources,
                        _commonStates,
                        _sceneGpuCache.BoneConnectionResources,
                        _state.CameraState,
                        _state.LightState,
                        modelTransform);
                }

                if (_state.ViewportOptions.ShowBones)
                {
                    var selectedBoneIndex = ResolveSelectedBoneIndex(
                        _state.Scene?.Skeleton,
                        _state.Selection.SelectedBoneName);
                    var boneAxisResources = _sceneGpuCache.GetBoneAxisResources(
                        _deviceResources,
                        _state.ViewportOptions.BoneAxisLength,
                        selectedBoneIndex);

                    if (boneAxisResources != null)
                    {
                        previewShader.DrawLines(
                            _deviceResources,
                            _swapChainResources,
                            _commonStates,
                            boneAxisResources,
                            _state.CameraState,
                            _state.LightState,
                            modelTransform);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                RendererDiagnostics.WriteException("RenderThreadWorker.RenderFrameIfPossible", ex);
                deviceContext.ClearRenderTargetView(renderTargetView, new Color4(0.45f, 0.05f, 0.08f, 1.0f));
            }
        }

        swapChain.Present(_state.IsInteracting ? 0u : 1u, PresentFlags.None);
    }

    // Resolves the selected bone index based on the current skeleton and the name of the selected bone. Returns -1 if no valid selection is found.
    private static int ResolveSelectedBoneIndex(RenderSkeletonSnapshot? skeleton, string? selectedBoneName)
    {
        if (skeleton == null || string.IsNullOrWhiteSpace(selectedBoneName))
        {
            return -1;
        }

        return skeleton.BoneIndicesByName.TryGetValue(selectedBoneName, out var boneIndex)
            ? boneIndex
            : -1;
    }
}
