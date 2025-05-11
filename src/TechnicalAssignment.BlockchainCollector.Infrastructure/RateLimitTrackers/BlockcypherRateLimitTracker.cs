using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.RateLimitTrackers;

internal class BlockcypherRateLimitTracker : IBlockcypherRateLimitTracker
{
    private readonly TimeProvider _timeProvider;
    private readonly IRedisConnectionPoolManager _redisConnectionPoolManager;
    private readonly BlockcypherRateLimitOptions _blockcypherRateLimitOptions;

    public BlockcypherRateLimitTracker(
        TimeProvider timeProvider,
        IOptions<BlockcypherRateLimitOptions> blockcypherRateLimitOptions,
        IRedisConnectionPoolManager redisConnectionPoolManager)
    {
        _timeProvider = timeProvider;
        _redisConnectionPoolManager = redisConnectionPoolManager;
        _blockcypherRateLimitOptions = blockcypherRateLimitOptions.Value;
    }

    public Task<bool> AcquireTokenAsync(CancellationToken cancellationToken) =>
        TryAddRequestTokenAsync(cancellationToken);

    private async Task<bool> TryAddRequestTokenAsync(CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var hashField = (RedisValue)Guid.NewGuid().ToString();

        var transaction = _redisConnectionPoolManager.GetConnection().GetDatabase().CreateTransaction();

        foreach (var rule in _blockcypherRateLimitOptions.Rules)
        {
            var expireTtl = GetTtl(rule, utcNow);

            var tokensKey = BuildTokensKey(rule.Name);

            transaction.AddCondition(Condition.HashLengthLessThan(tokensKey, rule.Requests));

#pragma warning disable CS4014
            transaction.HashSetAsync(tokensKey, hashField, 0, When.NotExists);
            transaction.KeyExpireAsync(tokensKey, expireTtl, ExpireWhen.HasNoExpiry);
#pragma warning restore CS4014
        }

        var success = await transaction.ExecuteAsync().WaitAsync(cancellationToken);

        return success;
    }

    private TimeSpan GetTtl(RateLimitRule rule, DateTimeOffset utcNow)
    {
        return rule.Period switch
        {
            Period.Hour => new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, utcNow.Offset).AddHours(1) - utcNow,
            Period.Second => TimeSpan.FromSeconds(1),
            _ => throw new NotImplementedException(),
        };
    }

    private static RedisKey BuildTokensKey(string ruleName) =>
        $"blockchain:rate-limiter:{ruleName}:tokens";

    public async Task<TimeSpan> GetCooldownTimeAsync(CancellationToken cancellationToken)
    {
        var maxTtl = TimeSpan.Zero;
        var utcNow = _timeProvider.GetUtcNow();

        var database = _redisConnectionPoolManager.GetConnection().GetDatabase();

        var ruleGetLenghtTaskMap = _blockcypherRateLimitOptions.Rules
            .Select(r => (Rule: r, Task: database.HashLengthAsync(BuildTokensKey(r.Name))));

        foreach (var (rule, getLenghtTask) in ruleGetLenghtTaskMap)
        {
            var lenght = await getLenghtTask;

            if (lenght < rule.Requests)
                continue;

            var ruleTtl = GetTtl(rule, utcNow);

            if (ruleTtl > maxTtl)
                maxTtl = ruleTtl;
        }

        return maxTtl;
    }
}
