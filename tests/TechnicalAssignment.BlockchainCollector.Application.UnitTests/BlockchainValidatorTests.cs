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
        // Arrange
        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = Array.Empty<Coin>() });

        // Act
        var result = Subject.Validate("btc", "main");

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Validation.InvalidConfiguration");
        resultError.Message.ShouldBe("Validation config is invalid");
    }

    [Fact]
    public void Validate_CoinIsNotSupported_ReturnsFailedResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();

        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = _fixture.Create<string>(), Chain = chain }] });

        // Act
        var result = Subject.Validate(coin, chain);

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Validation.CoinNotSupported");
        resultError.Message.ShouldBe($"The coin '{coin}' is not supported");
    }

    [Fact]
    public void Validate_ChainIsNotSupported_ReturnsFailedResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();

        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = coin, Chain = _fixture.Create<string>() }] });

        // Act
        var result = Subject.Validate(coin, chain);

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Validation.ChainNotSupported");
        resultError.Message.ShouldBe($"The chain '{chain}' is not supported");
    }

    [Fact]
    public void Validate_CoinAndChainAreSupported_ReturnsSuccessResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();

        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = coin, Chain = chain }] });

        // Act
        var result = Subject.Validate(coin, chain);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CoinAndChainAreUpperCased_ReturnsSuccessResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();

        _mocker.GetMock<IOptions<BlockchainOptions>>()
            .Setup(o => o.Value)
            .Returns(new BlockchainOptions { Coins = [new Coin { Code = coin, Chain = chain }] });

        // Act
        var result = Subject.Validate(coin.ToUpper(), chain.ToUpper());

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
