using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Infrastructure;
using TechnicalAssignment.BlockchainCollector.Tests.Common;

namespace TechnicalAssignment.BlockchainCollector.Application.IntegrationTests;

public class BlockchainsCollectorHostedServiceTests : ContainerizedWebAppTestBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private readonly WebApplicationFactory<Program> _applicationFactory;
    private readonly ManualResetEvent _manualResetEvent = new(false);
    private readonly BlockchainDto _blockchainDto;
    private volatile int _callsCount = 0;

    public BlockchainsCollectorHostedServiceTests()
    {
        _blockchainDto = Fixture.Build<BlockchainDto>()
            .With(b => b.Name, "btc.main")
            .Create();

        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);

        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .SetupSequence(s => s.Content)
          .Returns(_blockchainDto)
          .Returns(_blockchainDto)
          .Returns(default(BlockchainDto));

        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>())
            .Callback(() =>
            {
                if (Interlocked.Increment(ref _callsCount) >= 2)
                    _manualResetEvent.Set();
            });

        _applicationFactory = CreateApplicationFactory<Program>();
    }

    [Fact]
    public void CollectBlockchainsAsync_NewBlockchainRecieved_SavesBlockInDb()
    {
        // Arrange
        using var scope = _applicationFactory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<BlockchainDbContext>();

        // Act
        _manualResetEvent.WaitOne(Timeout);

        // Assert
        dbContext.Blockchains
            .Any(b => b.Name == _blockchainDto.Name && b.Hash == _blockchainDto.Hash)
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