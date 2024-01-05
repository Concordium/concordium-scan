using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Entities;

public sealed class TokenEvent
{
    [ID]
    public long Id { get; init; }
    public ulong ContractIndex { get; init; }
    public ulong ContractSubIndex { get; init; }
    public string TokenId { get; init; }
    public CisEvent Event { get; init; }

    public TokenEvent(
        ulong contractIndex,
        ulong contractSubIndex,
        string tokenId,
        CisEvent @event)
    {
        ContractIndex = contractIndex;
        ContractSubIndex = contractSubIndex;
        TokenId = tokenId;
        Event = @event;
    }
    
    public Transaction? GetTransaction(GraphQlDbContext dbContext) => 
        dbContext.Transactions.AsNoTracking().SingleOrDefault(t => t.TransactionHash == Event.TransactionHash);
}
