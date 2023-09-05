using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Initial event stored when a smart contract is created
/// These are non mutable values through the lifetime of the smart contract.
/// </summary>
public sealed class Contract
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; } = null!;
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public AccountAddress Creator { get; init; } = null!;
    public ImportSource Source { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private Contract()
    {}

    internal Contract(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        ContractAddress contractAddress,
        AccountAddress creator,
        ImportSource source)
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        EventIndex = eventIndex;
        Creator = creator;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Source = source;
    }
}