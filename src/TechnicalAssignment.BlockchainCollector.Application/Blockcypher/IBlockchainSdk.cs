using Refit;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;

namespace TechnicalAssignment.BlockchainCollector.Application.Blockcypher;

public interface IBlockchainSdk
{
    [Get("/v1/{coin}/{chain}")]
    Task<IApiResponse<BlockchainDto>> GetBlockchainAsync(string coin, string chain, CancellationToken cancellationToken);
}
