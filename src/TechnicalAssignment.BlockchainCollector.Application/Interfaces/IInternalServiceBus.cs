namespace TechnicalAssignment.BlockchainCollector.Application.Interfaces;

public interface IInternalServiceBus<TEvent>
{
    Task PushAsync(TEvent @event, CancellationToken cancellationToken);
    IAsyncEnumerable<TEvent> ReadAllAsync(CancellationToken cancellationToken);
}
