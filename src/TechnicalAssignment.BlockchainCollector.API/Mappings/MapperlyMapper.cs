using Riok.Mapperly.Abstractions;
using TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;
using TechnicalAssignment.BlockchainCollector.Application.Pagination;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.API.Mappings;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName, EnumMappingIgnoreCase = true)]
public partial class MapperlyMapper : IMapper
{
    [MapperIgnoreSource(nameof(Blockchain.Id))]
    public partial BlockchainResponse Map(Blockchain blockchain);

    public partial BlockchainHistoryResponse Map(PagedItems<Blockchain> pagedItems);

    [MapperIgnoreSource(nameof(Blockchain.Id))]
    private partial TechnicalAssignment.BlockchainCollector.API.Contracts.Blockchain MapBlockchain(Blockchain blockchain);
}