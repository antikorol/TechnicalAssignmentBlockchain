using Microsoft.Extensions.Options;
using TechnicalAssignment.BlockchainCollector.Application.Configurations;
using TechnicalAssignment.BlockchainCollector.Application.Validators;
using TechnicalAssignment.BlockchainCollector.Domain.Errors;

namespace TechnicalAssignment.BlockchainCollector.Application.UnitTests;

public class BlockchainValidatorTests
{
    private readonly AutoMocker _mocker;
    private readonly Fixture _fixture;
    private readonly Lazy<BlockchainValidator> _subjectLazy;

    private BlockchainValidator Subject => _subjectLazy.Value;

    public BlockchainValidatorTests()
    {
        _mocker = new AutoMocker();
        _fixture = new Fixture();
        _subjectLazy = new Lazy<BlockchainValidator>(() => _mocker.CreateInstance<BlockchainValidator>());
    }

    [Fact]
    public void Validate_NoCoinsConfigured_ReturnsFailedResult()
    {
        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = Array.Empty<Coin>() });

        var result = Subject.Validate("btc", "main");

        result.IsFailed.ShouldBeTrue();
        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Validation.InvalidConfiguration");
        resultError.Message.ShouldBe("Validation config is invalid");
    }

    [Fact]
    public void Validate_CoinIsNotSupported_ReturnsFailedResult()
    {
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = _fixture.Create<string>(), Chain = chain }] });

        var result = Subject.Validate(coin, chain);

        result.IsFailed.ShouldBeTrue();
        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Validation.CoinNotSupported");
        resultError.Message.ShouldBe($"The coin '{coin}' is not supported");
    }

    [Fact]
    public void Validate_ChainIsNotSupported_ReturnsFailedResult()
    {
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = coin, Chain = _fixture.Create<string>() }] });

        var result = Subject.Validate(coin, chain);

        result.IsFailed.ShouldBeTrue();
        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Validation.ChainNotSupported");
        resultError.Message.ShouldBe($"The chain '{chain}' is not supported");
    }

    [Fact]
    public void Validate_CoinAndChainAreSupported_ReturnsSuccessResult()
    {
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = coin, Chain = chain }] });

        var result = Subject.Validate(coin, chain);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CoinAndChainAreUpperCased_ReturnsSuccessResult()
    {
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = coin, Chain = chain }] });

        var result = Subject.Validate(coin.ToUpper(), chain.ToUpper());

        result.IsSuccess.ShouldBeTrue();
    }
}
