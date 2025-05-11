using MediatR;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Domain.Events;

namespace TechnicalAssignment.BlockchainCollector.Application.Handlers;

internal class BlockRecievedHandler : INotificationHandler<BlockReceived>
{
    private readonly IInternalServiceBus<BlockReceived> _serviceBus;

    public BlockRecievedHandler(IInternalServiceBus<BlockReceived> serviceBus)
    {
        _serviceBus = serviceBus;
    }

    public Task Handle(BlockReceived notification, CancellationToken cancellationToken) =>
        _serviceBus.PushAsync(notification, cancellationToken);
}
