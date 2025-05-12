using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Tests.Common;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using System.Threading.Tasks;
using TechnicalAssignment.BlockchainCollector.Infrastructure;

namespace TechnicalAssignment.BlockchainCollector.Application.IntegrationTests;

public class BlockchainServiceTests : ContainerizedWebAppTestBase
{
    private readonly WebApplicationFactory<Program> _applicationFactory;

    public BlockchainServiceTests()
    {
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(default(BlockchainDto));
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

        _applicationFactory = CreateApplicationFactory<Program>();
    }

    [Fact]
    public async Task CollectBlockchainsAsync_NewBlockchainRecieved_SavesBlockInDb()
    {
        // Arrange
        var coin = "BTC";
        var chain = "main";

        var dto = Fixture.Build<BlockchainDto>()
            .With(d => d.Name, $"{coin}.{chain}")
            .Create();

        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

        using var scope = _applicationFactory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<BlockchainDbContext>();
        var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

        // Act
        var result = await blockchainService.GetLastBlockAsync(coin, chain, Token);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(dto.Name);
        result.Value.Hash.ShouldBe(dto.Hash);
        await Task.Delay(TimeSpan.FromSeconds(5));
        dbContext.Blockchains
            .Any(b => b.Name == dto.Name && b.Hash == dto.Hash)
            .ShouldBeTrue();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RemoveRegistration(services, typeof(IBlockchainSdk));
        services.AddSingleton<IBlockchainSdk>(Mocker.Get<IBlockchainSdk>());
    }

    protected override void OnDispose()
    {
        _applicationFactory.Dispose();
    }
}