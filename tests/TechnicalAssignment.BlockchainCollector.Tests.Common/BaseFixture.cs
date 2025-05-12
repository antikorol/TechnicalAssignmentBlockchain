using AutoFixture;
using Moq.AutoMock;

namespace TechnicalAssignment.BlockchainCollector.Tests.Common;

public class BaseFixture<TSubject> : BaseFixture
    where TSubject : class
{
    private readonly Lazy<TSubject> _subjectLazy;

    protected BaseFixture() =>
        _subjectLazy = new Lazy<TSubject>(() => GetSubject<TSubject>());

    protected TSubject Subject => _subjectLazy.Value;
}

public class BaseFixture
{
    protected AutoMocker Mocker { get; } = new AutoMocker();
    protected Fixture Fixture { get; } = new Fixture();
    protected CancellationToken Token { get; } = CancellationToken.None;

    protected TSubject GetSubject<TSubject>() where TSubject : class =>
        Mocker.CreateInstance<TSubject>();
}