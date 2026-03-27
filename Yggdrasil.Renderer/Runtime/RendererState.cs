using Yggdrasil.Renderer.Api;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Runtime;

internal sealed class RendererState
{
    public IRenderSurfaceHost? SurfaceHost { get; set; }

    public RenderSceneSnapshot? Scene { get; set; }

    public RenderSelectionState Selection { get; set; } = RenderSelectionState.Empty;

    public OrbitCameraState CameraState { get; set; } = OrbitCameraState.Default;

    public Vector2i PixelSize { get; set; } = Vector2i.Zero;

    public bool SurfaceDirty { get; set; }

    public bool GeometryDirty { get; set; }

    public bool MaterialsDirty { get; set; }

    public bool IsInteracting { get; set; }
}
