using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;
using TechnicalAssignment.BlockchainCollector.Domain.Events;
using TechnicalAssignment.BlockchainCollector.Tests.Common;
using TechnicalAssignment.BlockchainCollector.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using Refit;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace TechnicalAssignment.BlockchainCollector.Application.IntegrationTests;

public class BlockRecievedHandlerIntegrationTests : ContainerizedWebAppTestBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private readonly WebApplicationFactory<Program> _applicationFactory;
    private readonly ManualResetEvent _manualResetEvent = new(false);

    public BlockRecievedHandlerIntegrationTests()
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

    protected override void ConfigureServices(IServiceCollection services)
    {
        RemoveRegistration(services, typeof(IBlockchainSdk));
        services.AddSingleton<IBlockchainSdk>(Mocker.Get<IBlockchainSdk>());

        services.Decorate<IUnitOfWork>(uof => new UintOfWorDecorator(uof, _manualResetEvent));
    }

    protected override void OnDispose()
    {
        _applicationFactory.Dispose();
    }

    [Fact]
    public async Task Publish_NewBlockReceived_SavesBlockInDb()
    {
        // Arrange
        using var scope = _applicationFactory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        using var dbContext = scope.ServiceProvider.GetRequiredService<BlockchainDbContext>();

        const string coin = "btc";
        const string chain = "main";
        var name = $"{coin}.{chain}";

        var blockchain = Fixture.Build<Blockchain>()
            .With(x => x.Name, name)
            .With(x => x.CreatedAt, DateTime.UtcNow)
            .Create();

        // Act
        await mediator.Publish(new BlockReceived(blockchain), Token);
        _manualResetEvent.WaitOne(Timeout);
        _manualResetEvent.Reset();

        // Assert
        dbContext.Blockchains
            .AsNoTracking()
            .Any(b => b.Name.ToLower() == blockchain.Name.ToLower()
                && b.Hash == blockchain.Hash)
            .ShouldBeTrue();
    }

    private class UintOfWorDecorator : IUnitOfWork
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ManualResetEvent _manualResetEvent;

        public UintOfWorDecorator(IUnitOfWork unitOfWork, ManualResetEvent manualResetEvent)
        {
            _unitOfWork = unitOfWork;
            _manualResetEvent = manualResetEvent;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _manualResetEvent.Set();
        }
    }
}