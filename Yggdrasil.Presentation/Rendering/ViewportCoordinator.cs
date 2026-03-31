using System.Threading;
using System.Threading.Tasks;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Renderer.Api;
using Yggdrasil.Renderer.Camera;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Presentation.Rendering;

/// <summary>
/// Handles coordination between the application and the renderer host, managing the current scene, camera state, light state and viewport options.
/// </summary>
public sealed class ViewportCoordinator
{
    private readonly IRendererHost _rendererHost; // Injected renderer host that this coordinator will manage.
    private readonly SceneToRenderSnapshotMapper _sceneMapper; // Mapper to convert application scene models to renderer snapshots.
    private OrbitCameraState _currentCameraState = OrbitCameraState.Default; // Current camera state, initialized to default. Updated when SetScene is called or when UpdateCameraState is called.
    private OrbitLightState _currentLightState = OrbitLightState.Default; // Current light state, initialized to default. Updated when SetScene is called or when UpdateLightState is called.
    private ViewportRenderOptions _currentViewportOptions = ViewportRenderOptions.Default; // Current viewport options, initialized to default. Updated when SetScene is called or when UpdateViewportOptions is called.

    public ViewportCoordinator(IRendererHost rendererHost, SceneToRenderSnapshotMapper sceneMapper)
    {
        _rendererHost = rendererHost ?? throw new System.ArgumentNullException(nameof(rendererHost));
        _sceneMapper = sceneMapper ?? throw new System.ArgumentNullException(nameof(sceneMapper));
    }

    public ValueTask AttachSurfaceAsync(SwapChainPanelSurfaceHost surfaceHost, CancellationToken cancellationToken = default)
    {
        System.ArgumentNullException.ThrowIfNull(surfaceHost);

        return _rendererHost.AttachSurfaceAsync(surfaceHost, cancellationToken);
    }

    public OrbitCameraState CurrentCameraState => _currentCameraState;

    public OrbitLightState CurrentLightState => _currentLightState;

    public ViewportRenderOptions CurrentViewportOptions => _currentViewportOptions;

    public ValueTask DetachSurfaceAsync(CancellationToken cancellationToken = default)
    {
        return _rendererHost.DetachSurfaceAsync(cancellationToken);
    }

    // Sets the current scene to be rendered. If resetCamera is true, the camera will be reset to a default position framing the new scene. The renderer host will be updated with the new scene, camera state, light state and viewport options.
    public void SetScene(SceneModel? scene, bool resetCamera = true)
    {
        // If scene is null, clear the host
        // Otherwise map the new one
        var snapshot = scene == null ? null : _sceneMapper.MapScene(scene);
        _rendererHost.SetScene(snapshot);

        // If resetting the camera, set it to a default position framing the new scene (or the default if scene is null)
        if (resetCamera)
        {
            _currentCameraState = snapshot == null
                ? OrbitCameraState.Default
                : OrbitCameraMath.CreateFramedState(snapshot.Bounds);
        }

        // Reset light state and viewport options to default when setting a new scene
        _rendererHost.SetCameraState(_currentCameraState, false);
        _rendererHost.SetLightState(_currentLightState, false);
        _rendererHost.SetViewportOptions(_currentViewportOptions);
    }

    public void UpdateMaterial(string materialName, SourceMaterialSettings material)
    {
        System.ArgumentNullException.ThrowIfNull(material);

        _rendererHost.UpdateMaterial(_sceneMapper.MapMaterial(materialName, material));
    }

    public void UpdateSelection(RenderSelectionState selection)
    {
        System.ArgumentNullException.ThrowIfNull(selection);

        _rendererHost.SetSelection(selection);
    }

    public void UpdateCameraState(OrbitCameraState cameraState, bool isInteracting)
    {
        System.ArgumentNullException.ThrowIfNull(cameraState);

        _currentCameraState = cameraState;
        _rendererHost.SetCameraState(cameraState, isInteracting);
    }

    public void UpdateLightState(OrbitLightState lightState, bool isInteracting)
    {
        System.ArgumentNullException.ThrowIfNull(lightState);

        _currentLightState = lightState;
        _rendererHost.SetLightState(lightState, isInteracting);
    }

    public void UpdateViewportOptions(ViewportRenderOptions viewportOptions)
    {
        System.ArgumentNullException.ThrowIfNull(viewportOptions);

        _currentViewportOptions = viewportOptions;
        _rendererHost.SetViewportOptions(viewportOptions);
    }

    public void Resize(Vector2i pixelSize)
    {
        _rendererHost.Resize(pixelSize);
    }
}
