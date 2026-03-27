using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using SharpGen.Runtime;
using Vortice.DXGI;
using Yggdrasil.Renderer.Api;

namespace Yggdrasil.Presentation.Rendering;

public sealed class SwapChainPanelSurfaceHost : IRenderSurfaceHost
{
    public SwapChainPanelSurfaceHost(SwapChainPanel panel)
    {
        Panel = panel ?? throw new ArgumentNullException(nameof(panel));
    }

    public SwapChainPanel Panel { get; }

    public ValueTask BindSwapChainAsync(IDXGISwapChain1 swapChain, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(swapChain);

        return RunOnUiThreadAsync(() => SetSwapChain(swapChain, suppressUnavailable: false), cancellationToken, suppressUnavailable: false);
    }

    public ValueTask ClearSwapChainAsync(CancellationToken cancellationToken = default)
    {
        return RunOnUiThreadAsync(() => SetSwapChain(null, suppressUnavailable: true), cancellationToken, suppressUnavailable: true);
    }

    private ValueTask RunOnUiThreadAsync(Action action, CancellationToken cancellationToken, bool suppressUnavailable)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dispatcherQueue = Panel.DispatcherQueue
            ?? (suppressUnavailable
                ? null
                : throw new InvalidOperationException("SwapChainPanel does not have an associated DispatcherQueue."));

        if (dispatcherQueue == null)
        {
            return ValueTask.CompletedTask;
        }

        if (dispatcherQueue.HasThreadAccess)
        {
            TryInvokeAction(action, suppressUnavailable);
            return ValueTask.CompletedTask;
        }

        var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
        }

        if (!dispatcherQueue.TryEnqueue(() =>
            {
                cancellationRegistration.Dispose();

                if (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled(cancellationToken);
                    return;
                }

                try
                {
                    TryInvokeAction(action, suppressUnavailable);
                    completionSource.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    completionSource.TrySetException(ex);
                }
            }))
        {
            cancellationRegistration.Dispose();
            return suppressUnavailable
                ? ValueTask.CompletedTask
                : ValueTask.FromException(new InvalidOperationException("Failed to enqueue swap-chain work to the UI thread."));
        }

        return new ValueTask(completionSource.Task);
    }

    private void SetSwapChain(IDXGISwapChain1? swapChain, bool suppressUnavailable)
    {
        try
        {
            using var panelComObject = new ComObject(Panel);
            using var nativePanel = panelComObject.QueryInterfaceOrNull<Vortice.WinUI.ISwapChainPanelNative>()
                ?? throw new InvalidOperationException("Failed to acquire the native SwapChainPanel interface.");

            nativePanel.SetSwapChain(swapChain);
        }
        catch (Exception ex) when (suppressUnavailable && IsExpectedShutdownException(ex))
        {
        }
    }

    private static void TryInvokeAction(Action action, bool suppressUnavailable)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (suppressUnavailable && IsExpectedShutdownException(ex))
        {
        }
    }

    private static bool IsExpectedShutdownException(Exception exception)
    {
        return exception is InvalidOperationException
            or ObjectDisposedException
            or COMException
            or SharpGenException;
    }
}
