using System.Threading.Channels;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.ServiceBus;

internal class InternalServiceBus<TEvent> : IInternalServiceBus<TEvent>, IDisposable
{
    private readonly Channel<TEvent> _channel;

    public InternalServiceBus()
    {
        _channel = Channel.CreateBounded<TEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task PushAsync(TEvent entity, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(entity, cancellationToken);
    }

    public IAsyncEnumerable<TEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    public void Dispose()
    {
        _channel.Writer.TryComplete();
    }
}
