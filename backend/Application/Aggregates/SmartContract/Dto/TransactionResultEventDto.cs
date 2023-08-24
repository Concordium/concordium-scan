using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.SmartContract.Dto;

/// <summary>
/// Used as a Dto when <see cref="TransactionResultEvent"/> from a block height is queried.
/// </summary>
public class TransactionResultEventDto
{
    public int BlockHeight { get; init; }
    public TransactionTypeUnion TransactionType { get; init; }
    public AccountAddress? TransactionSender { get; init; }
    public string TransactionHash { get; init; }
    public uint TransactionIndex { get; init; }
    public uint TransactionEventIndex { get; init; }
    public TransactionResultEvent Event { get; init; }
    
    /// <summary>
    /// Needed for Dapper
    /// </summary>
#pragma warning disable CS8618
    public TransactionResultEventDto()
#pragma warning restore CS8618
    {}
}