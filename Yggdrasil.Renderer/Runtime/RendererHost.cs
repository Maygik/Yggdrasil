using System;
using System.Threading;
using System.Threading.Tasks;
using Yggdrasil.Renderer.Api;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Runtime;

public sealed class RendererHost : IRendererHost
{
    private readonly RendererMailbox _mailbox;
    private readonly RenderThreadWorker _worker;
    private readonly CancellationTokenSource _shutdownCancellationSource = new();
    private readonly Task _workerTask;
    private int _disposeState;

    public RendererHost()
    {
        _mailbox = new RendererMailbox();
        _worker = new RenderThreadWorker(_mailbox);
        _workerTask = Task.Factory.StartNew(
                () => _worker.RunAsync(_shutdownCancellationSource.Token),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();
    }

    // Attaches a render surface to the renderer. The renderer will start rendering to this surface until it is detached or another surface is attached.
    public ValueTask AttachSurfaceAsync(IRenderSurfaceHost surfaceHost, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(surfaceHost);

        EnsureNotDisposed();
        _mailbox.Enqueue(new AttachSurfaceCommand(surfaceHost));
        return ValueTask.CompletedTask;
    }

    // Detaches the currently attached render surface, if any. The renderer will stop rendering until a new surface is attached.
    public ValueTask DetachSurfaceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureNotDisposed();
        _mailbox.Enqueue(new DetachSurfaceCommand());
        return ValueTask.CompletedTask;
    }

    // Resizes the render surface to the specified pixel size. This should be called whenever the surface size changes to ensure correct rendering.
    public void Resize(Vector2i pixelSize)
    {
        EnsureNotDisposed();
        _mailbox.Enqueue(new ResizeCommand(pixelSize));
    }

    // Loads a new scene into the renderer. The renderer will update its geometry and materials based on the contents of the scene snapshot. If null is passed, the renderer will clear the current scene.
    public void SetScene(RenderSceneSnapshot? scene)
    {
        EnsureNotDisposed();
        _mailbox.Enqueue(new LoadSceneCommand(scene));
    }

    // Updates the properties of a material in the renderer. The material snapshot must have a valid ID that corresponds to an existing material in the current scene.
    // The renderer will apply the updated properties to the material and mark it for re-rendering.
    public void UpdateMaterial(RenderMaterialSnapshot material)
    {
        ArgumentNullException.ThrowIfNull(material);

        EnsureNotDisposed();
        _mailbox.Enqueue(new UpdateMaterialCommand(material));
    }

    // Updates the current selection state in the renderer. The renderer will update the visual appearance of selected objects based on the provided selection state.
    public void SetSelection(RenderSelectionState selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        EnsureNotDisposed();
        _mailbox.Enqueue(new SetSelectionCommand(selection));
    }

    // Updates the current camera state in the renderer.
    // isInteracting indicates whether it's currently being moved (e.g. by user input)
    public void SetCameraState(OrbitCameraState cameraState, bool isInteracting)
    {
        ArgumentNullException.ThrowIfNull(cameraState);

        EnsureNotDisposed();
        _mailbox.Enqueue(new SetCameraStateCommand(cameraState, isInteracting));
    }

    // Disposes the renderer host and releases all resources. This will stop the render thread and clean up any associated resources. After disposal, the renderer host should not be used.
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            await _workerTask.ConfigureAwait(false);
            return;
        }

        try
        {
            _worker.Stop();
            await _workerTask.ConfigureAwait(false);
        }
        finally
        {
            _mailbox.Complete();
            _shutdownCancellationSource.Cancel();
            _shutdownCancellationSource.Dispose();
        }
    }

    // Throws an ObjectDisposedException if the renderer host has already been disposed.
    private void EnsureNotDisposed()
    {
        if (_disposeState != 0)
        {
            throw new ObjectDisposedException(nameof(RendererHost));
        }
    }
}
