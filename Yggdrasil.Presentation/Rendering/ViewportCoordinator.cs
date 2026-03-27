using System.Threading;
using System.Threading.Tasks;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Renderer.Api;
using Yggdrasil.Renderer.Camera;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Presentation.Rendering;

public sealed class ViewportCoordinator
{
    private readonly IRendererHost _rendererHost;
    private readonly SceneToRenderSnapshotMapper _sceneMapper;
    private OrbitCameraState _currentCameraState = OrbitCameraState.Default;

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

    public ValueTask DetachSurfaceAsync(CancellationToken cancellationToken = default)
    {
        return _rendererHost.DetachSurfaceAsync(cancellationToken);
    }

    public void SetScene(SceneModel? scene)
    {
        var snapshot = scene == null ? null : _sceneMapper.MapScene(scene);
        _rendererHost.SetScene(snapshot);

        _currentCameraState = snapshot == null
            ? OrbitCameraState.Default
            : OrbitCameraMath.CreateFramedState(snapshot.Bounds);
        _rendererHost.SetCameraState(_currentCameraState, false);
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

    public void Resize(Vector2i pixelSize)
    {
        _rendererHost.Resize(pixelSize);
    }
}
