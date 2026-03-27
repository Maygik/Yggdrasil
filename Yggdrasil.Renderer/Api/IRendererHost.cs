using System;
using System.Threading;
using System.Threading.Tasks;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Api;

public interface IRendererHost : IAsyncDisposable
{
    ValueTask AttachSurfaceAsync(IRenderSurfaceHost surfaceHost, CancellationToken cancellationToken = default);

    ValueTask DetachSurfaceAsync(CancellationToken cancellationToken = default);

    void Resize(Vector2i pixelSize);

    void SetScene(RenderSceneSnapshot? scene);

    void UpdateMaterial(RenderMaterialSnapshot material);

    void SetSelection(RenderSelectionState selection);

    void SetCameraState(OrbitCameraState cameraState, bool isInteracting);
}
