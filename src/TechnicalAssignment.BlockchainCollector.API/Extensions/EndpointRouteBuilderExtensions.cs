using FluentResults;
using Microsoft.AspNetCore.Mvc;
using TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;
using TechnicalAssignment.BlockchainCollector.API.Mappings;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace TechnicalAssignment.BlockchainCollector.API.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/public/{coin}/{chain}",
            async (
                [FromRoute] string coin,
                [FromRoute] string chain,
                CancellationToken token,
                IMapper mapper,
                IBlockchainService blockchainService) =>
            {
                var result = await blockchainService.GetLastBlockAsync(coin, chain, token);

                return result.IsSuccess
                    ? Results.Ok(mapper.Map(result.Value))
                    : BadRequest(result);
            })
           .WithName("GetCurrentBlockchainState")
           .WithOpenApi()
           .Produces<BlockchainResponse>(StatusCodes.Status200OK)
           .Produces<BadRequestResponse>(StatusCodes.Status404NotFound)
           .Produces<BadRequestResponse>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("/api/public/{coin}/{chain}/history",
            async (
                [FromRoute] string coin,
                [FromRoute] string chain,
                CancellationToken token,
                IMapper mapper,
                IBlockchainService blockchainService,
                [FromQuery] uint offset = 0,
                [FromQuery] uint limit = 10) =>
            {
                var result = await blockchainService.LoadHistoryAsync(coin, chain, offset, limit, token);

                return result.IsSuccess
                    ? Results.Ok(mapper.Map(result.Value))
                    : BadRequest(result);
            })
           .WithName("GetBlockchainHistory")
           .WithOpenApi()
           .Produces<BlockchainHistoryResponse>(StatusCodes.Status200OK)
           .Produces<BadRequestResponse>(StatusCodes.Status400BadRequest);

        return routeBuilder;

        static IResult BadRequest<TResult>(Result<TResult> result)
        {
            if (result.HasError<Domain.Errors.Error>(out var errors))
            {
                var error = errors.First();

                return
                    error.Type == Domain.Errors.ErrorType.NotFound
                    ? Results.NotFound(new BadRequestResponse(error.Code, error.Message))
                    : Results.BadRequest(new BadRequestResponse(error.Code, error.Message));
            }

            return Results.BadRequest(new BadRequestResponse("Unknown", string.Empty));
        }
    }
}