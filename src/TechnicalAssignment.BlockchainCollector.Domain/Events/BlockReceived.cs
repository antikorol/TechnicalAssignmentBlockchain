using MediatR;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Domain.Events;

public record BlockReceived(Blockchain Block) : INotification;
