using Application.Aggregates.SmartContract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Initial event stored when a smart contract is created
/// These are non mutable values through the lifetime of the smart contract.
/// </summary>
public sealed class SmartContract
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; }
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public AccountAddress Creator { get; init; }
    public ImportSource Source { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private SmartContract()
#pragma warning restore CS8618
    {}

    internal SmartContract(
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