using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Yggdrasil.Renderer.Runtime;

internal sealed class RendererMailbox
{
    // Channel for sending commands from the main thread to the render thread
    private readonly Channel<RendererCommand> _channel = Channel.CreateUnbounded<RendererCommand>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    // Enqueue a command to be processed by the render thread
    public void Enqueue(RendererCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_channel.Writer.TryWrite(command))
        {
            throw new InvalidOperationException("Renderer mailbox is no longer accepting commands.");
        }
    }

    // Dequeue a command asynchronously, waiting if necessary until one is available
    public ValueTask<RendererCommand> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public bool TryDequeue(out RendererCommand? command)
    {
        return _channel.Reader.TryRead(out command);
    }

    // Signal that no more commands will be enqueued and the mailbox should complete
    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}
