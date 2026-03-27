using Yggdrasil.Renderer.Api;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Runtime;

internal abstract record RendererCommand;

internal sealed record AttachSurfaceCommand(IRenderSurfaceHost SurfaceHost) : RendererCommand;

internal sealed record DetachSurfaceCommand() : RendererCommand;

internal sealed record ResizeCommand(Vector2i PixelSize) : RendererCommand;

internal sealed record LoadSceneCommand(RenderSceneSnapshot? Scene) : RendererCommand;

internal sealed record UpdateMaterialCommand(RenderMaterialSnapshot Material) : RendererCommand;

internal sealed record SetSelectionCommand(RenderSelectionState Selection) : RendererCommand;

internal sealed record SetCameraStateCommand(OrbitCameraState CameraState, bool IsInteracting) : RendererCommand;

internal sealed record ShutdownCommand() : RendererCommand;
