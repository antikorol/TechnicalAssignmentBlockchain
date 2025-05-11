using Riok.Mapperly.Abstractions;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Application.Mappings;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName, EnumMappingIgnoreCase = true)]
public partial class MapperlyMapper : IMapper
{
    [MapperIgnoreTarget(nameof(Blockchain.Id))]
    public partial Blockchain Map(BlockchainDto dto, DateTime createdAt);
}