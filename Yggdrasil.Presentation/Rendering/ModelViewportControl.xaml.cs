using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yggdrasil.Types;
using Yggdrasil.Renderer.Scene;

namespace Yggdrasil.Presentation.Rendering;

public sealed partial class ModelViewportControl : UserControl
{
    private readonly SwapChainPanelSurfaceHost _surfaceHost;
    private readonly OrbitCameraController _cameraController;
    private ViewportCoordinator? _coordinator;
    private bool _isSurfaceAttached;

    public ModelViewportControl()
    {
        InitializeComponent();

        _surfaceHost = new SwapChainPanelSurfaceHost(ViewportSwapChainPanel);
        _cameraController = new OrbitCameraController();
        _cameraController.StateChanged += CameraController_StateChanged;
        _cameraController.Attach(ViewportSwapChainPanel);

        Loaded += ModelViewportControl_Loaded;
        Unloaded += ModelViewportControl_Unloaded;
        ViewportSwapChainPanel.SizeChanged += ViewportSwapChainPanel_SizeChanged;
        ViewportSwapChainPanel.CompositionScaleChanged += ViewportSwapChainPanel_CompositionScaleChanged;
    }

    public SwapChainPanel SwapChainPanel => ViewportSwapChainPanel;

    public void Connect(ViewportCoordinator coordinator)
    {
        ArgumentNullException.ThrowIfNull(coordinator);

        if (ReferenceEquals(_coordinator, coordinator))
        {
            QueueRefresh();
            return;
        }

        var wasAttached = _isSurfaceAttached;
        _coordinator = coordinator;
        SetCameraState(coordinator.CurrentCameraState);

        if (wasAttached)
        {
            QueueReattach();
            return;
        }

        QueueRefresh();
    }

    public void Disconnect()
    {
        _ = DisconnectAsync();
    }

    public void SetCameraState(OrbitCameraState cameraState)
    {
        ArgumentNullException.ThrowIfNull(cameraState);
        _cameraController.SetState(cameraState);
    }

    public Task DisconnectAsync()
    {
        var coordinator = _coordinator;
        _coordinator = null;

        if (coordinator == null)
        {
            return Task.CompletedTask;
        }

        return DetachAsync(coordinator);
    }

    private void ModelViewportControl_Loaded(object sender, RoutedEventArgs e)
    {
        QueueRefresh();
    }

    private void ModelViewportControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_coordinator == null)
        {
            return;
        }

        QueueDetach(_coordinator);
    }

    private void ViewportSwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        PushResize();
    }

    private void ViewportSwapChainPanel_CompositionScaleChanged(SwapChainPanel sender, object args)
    {
        PushResize();
    }

    private void CameraController_StateChanged(OrbitCameraState cameraState, bool isInteracting)
    {
        _coordinator?.UpdateCameraState(cameraState, isInteracting);
    }

    private void QueueRefresh()
    {
        _ = RefreshAsync();
    }

    private void QueueReattach()
    {
        _ = ReattachAsync();
    }

    private void QueueDetach(ViewportCoordinator coordinator)
    {
        _ = DetachAsync(coordinator);
    }

    private async Task RefreshAsync()
    {
        if (!IsLoaded || _coordinator == null)
        {
            return;
        }

        try
        {
            PushResize();

            if (_isSurfaceAttached)
            {
                return;
            }

            await _coordinator.AttachSurfaceAsync(_surfaceHost).ConfigureAwait(true);
            _isSurfaceAttached = true;
            PushResize();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task ReattachAsync()
    {
        if (!IsLoaded || _coordinator == null)
        {
            return;
        }

        try
        {
            if (_isSurfaceAttached)
            {
                await _coordinator.DetachSurfaceAsync().ConfigureAwait(true);
                _isSurfaceAttached = false;
            }

            PushResize();
            await _coordinator.AttachSurfaceAsync(_surfaceHost).ConfigureAwait(true);
            _isSurfaceAttached = true;
            PushResize();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task DetachAsync(ViewportCoordinator coordinator)
    {
        try
        {
            if (!_isSurfaceAttached)
            {
                return;
            }

            await coordinator.DetachSurfaceAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            _isSurfaceAttached = false;
        }
    }

    private void PushResize()
    {
        _coordinator?.Resize(GetPixelSize());
    }

    private Vector2i GetPixelSize()
    {
        var width = Math.Max(0, (int)Math.Round(ViewportSwapChainPanel.ActualWidth * ViewportSwapChainPanel.CompositionScaleX));
        var height = Math.Max(0, (int)Math.Round(ViewportSwapChainPanel.ActualHeight * ViewportSwapChainPanel.CompositionScaleY));

        return new Vector2i(width, height);
    }
}
