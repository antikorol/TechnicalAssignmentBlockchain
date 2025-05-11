using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Application.Mappings;

public interface IMapper
{
    Blockchain Map(BlockchainDto dto, DateTime createdAt);
}
