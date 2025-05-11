using TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;
using TechnicalAssignment.BlockchainCollector.Application.Pagination;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.API.Mappings;

public interface IMapper
{
    BlockchainResponse Map(Blockchain blockchain);
    BlockchainHistoryResponse Map(PagedItems<Blockchain> pagedItems);
}
