using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using TechnicalAssignment.BlockchainCollector.Infrastructure.RateLimitTrackers;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.UnitTests;

public class BlockcypherRateLimitTrackerTests
{
    private readonly AutoMocker _mocker;
    private readonly Fixture _fixture;
    private readonly Lazy<BlockcypherRateLimitTracker> _subjectLazy;

    private BlockcypherRateLimitTracker Subject => _subjectLazy.Value;

    public BlockcypherRateLimitTrackerTests()
    {
        _mocker = new AutoMocker();
        _fixture = new Fixture();
        _subjectLazy = new Lazy<BlockcypherRateLimitTracker>(() => _mocker.CreateInstance<BlockcypherRateLimitTracker>());

        _mocker.GetMock<IRedisConnectionPoolManager>()
            .Setup(m => m.GetConnection())
            .Returns(_mocker.Get<IConnectionMultiplexer>());
        _mocker.GetMock<IConnectionMultiplexer>()
           .Setup(m => m.GetDatabase(-1, null))
           .Returns(_mocker.Get<IDatabase>());
        _mocker.GetMock<IDatabase>()
          .Setup(m => m.CreateTransaction(null))
          .Returns(_mocker.Get<ITransaction>());
    }

    [Fact]
    public async Task AcquireTokenAsync_TransactionSucceeds_ReturnsTrue()
    {
        // Arrange
        _mocker.GetMock<IOptions<BlockcypherRateLimitOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockcypherRateLimitOptions
            {
                Rules = new[]
                {
                    new RateLimitRule { Name = "hour-limit", Period = Period.Hour, Requests = 5 }
                }
            });

        _mocker.GetMock<ITransaction>()
            .Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await Subject.AcquireTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeTrue();

        _mocker.GetMock<ITransaction>()
            .Verify(tx => tx.AddCondition(It.IsAny<Condition>()), Times.Once);
        _mocker.GetMock<ITransaction>()
            .Verify(tx => tx.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        _mocker.GetMock<ITransaction>()
            .Verify(tx => tx.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()), Times.Once);
        _mocker.GetMock<ITransaction>()
            .Verify(tx => tx.ExecuteAsync(It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetCooldownTimeAsync_AllRulesExceeded_ReturnsMaxTtl()
    {
        // Arrange
        _mocker
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

        _mocker.GetMock<IDatabase>()
            .Setup(db => db.HashLengthAsync(It.Is<RedisKey>(k => k.ToString().Contains("rule1")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _mocker.GetMock<IDatabase>()
            .Setup(db => db.HashLengthAsync(It.Is<RedisKey>(k => k.ToString().Contains("rule2")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        
        _mocker.GetMock<TimeProvider>()
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
        _mocker.GetMock<IOptions<BlockcypherRateLimitOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockcypherRateLimitOptions
            {
                Rules = new[]
                {
                     new RateLimitRule { Name = "rule1", Period = Period.Second, Requests = 10 },
                }
            });
        _mocker.GetMock<IDatabase>()
            .Setup(db => db.HashLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        var result = await Subject.GetCooldownTimeAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(TimeSpan.Zero);
    }
}
