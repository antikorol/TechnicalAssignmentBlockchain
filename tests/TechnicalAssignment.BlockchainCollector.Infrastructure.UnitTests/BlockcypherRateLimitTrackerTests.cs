using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using TechnicalAssignment.BlockchainCollector.Infrastructure.RateLimitTrackers;
using TechnicalAssignment.BlockchainCollector.Tests.Common;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.UnitTests;

public class BlockcypherRateLimitTrackerTests : BaseFixture
{
    private readonly Lazy<BlockcypherRateLimitTracker> _subjectLazy;

    private BlockcypherRateLimitTracker Subject => _subjectLazy.Value;

    public BlockcypherRateLimitTrackerTests()
    {
        _subjectLazy = new Lazy<BlockcypherRateLimitTracker>(() => GetSubject<BlockcypherRateLimitTracker>());

        Mocker.GetMock<IRedisConnectionPoolManager>()
            .Setup(m => m.GetConnection())
            .Returns(Mocker.Get<IConnectionMultiplexer>());
        Mocker.GetMock<IConnectionMultiplexer>()
           .Setup(m => m.GetDatabase(-1, null))
           .Returns(Mocker.Get<IDatabase>());
        Mocker.GetMock<IDatabase>()
          .Setup(m => m.CreateTransaction(null))
          .Returns(Mocker.Get<ITransaction>());
    }

    [Fact]
    public async Task AcquireTokenAsync_TransactionSucceeds_ReturnsTrue()
    {
        // Arrange
        Mocker.GetMock<IOptions<BlockcypherRateLimitOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockcypherRateLimitOptions
            {
                Rules = new[]
                {
                    new RateLimitRule { Name = "hour-limit", Period = Period.Hour, Requests = 5 }
                }
            });

        Mocker.GetMock<ITransaction>()
            .Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await Subject.AcquireTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeTrue();

        Mocker.GetMock<ITransaction>()
            .Verify(tx => tx.AddCondition(It.IsAny<Condition>()), Times.Once);
        Mocker.GetMock<ITransaction>()
            .Verify(tx => tx.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        Mocker.GetMock<ITransaction>()
            .Verify(tx => tx.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()), Times.Once);
        Mocker.GetMock<ITransaction>()
            .Verify(tx => tx.ExecuteAsync(It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetCooldownTimeAsync_AllRulesExceeded_ReturnsMaxTtl()
    {
        // Arrange
        Mocker
            .GetMock<IOptions<BlockcypherRateLimitOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockcypherRateLimitOptions
            {
                Rules = new[]
                {
                    new RateLimitRule { Name = "rule1", Period = Period.Second, Requests = 1 },
                    new RateLimitRule { Name = "rule2", Period = Period.Hour, Requests = 1 }
                }
            });

        Mocker.GetMock<IDatabase>()
            .Setup(db => db.HashLengthAsync(It.Is<RedisKey>(k => k.ToString().Contains("rule1")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        Mocker.GetMock<IDatabase>()
            .Setup(db => db.HashLengthAsync(It.Is<RedisKey>(k => k.ToString().Contains("rule2")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        
        Mocker.GetMock<TimeProvider>()
            .Setup(p => p.GetUtcNow())
            .Returns(new DateTimeOffset(2030, 1, 1, 1, 1, 0, TimeSpan.Zero));

        // Act
        var result = await Subject.GetCooldownTimeAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(TimeSpan.FromMinutes(59));
    }

    [Fact]
    public async Task GetCooldownTimeAsync_NoRulesExceeded_ReturnsZero()
    {
        // Arrange
        Mocker.GetMock<IOptions<BlockcypherRateLimitOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockcypherRateLimitOptions
            {
                Rules = new[]
                {
                     new RateLimitRule { Name = "rule1", Period = Period.Second, Requests = 10 },
                }
            });
        Mocker.GetMock<IDatabase>()
            .Setup(db => db.HashLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        var result = await Subject.GetCooldownTimeAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(TimeSpan.Zero);
    }
}
