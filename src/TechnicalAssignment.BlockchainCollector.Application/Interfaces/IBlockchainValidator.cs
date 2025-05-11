using FluentResults;

namespace TechnicalAssignment.BlockchainCollector.Application.Interfaces;

public interface IBlockchainValidator
{
    Result Validate(string coin, string chain);
}

