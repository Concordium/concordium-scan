using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.Contract.Dto;

/// <summary>
/// Used as a Dto when <see cref="TransactionRejectReason"/> from a block height is queried.
/// </summary>
public class TransactionRejectEventDto
{
    public int BlockHeight { get; init; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public TransactionTypeUnion TransactionType { get; init; }
    public AccountAddress? TransactionSender { get; init; }
    public string TransactionHash { get; init; }
    public uint TransactionIndex { get; init; }
    public TransactionRejectReason RejectedEvent { get; init; }
    
    /// <summary>
    /// Needed for Dapper
    /// </summary>
#pragma warning disable CS8618
    public TransactionRejectEventDto()
#pragma warning restore CS8618
    {}
}