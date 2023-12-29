using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import.EventLogs;
using Application.Api.GraphQL.Transactions;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Tokens;

public class TokenEvent
{
    [ID]
    public long Id { get; set; }
    public ulong ContractIndex { get; set; }
    public ulong ContractSubIndex { get; set; }
    public string TokenId { get; set; }
    public CisEvent Event { get; set; }

    public TokenEvent(
        ulong contractIndex,
        ulong contractSubIndex,
        string tokenId,
        long transactionId,
        CisEvent @event)
    {
        ContractIndex = contractIndex;
        ContractSubIndex = contractSubIndex;
        TokenId = tokenId;
        Event = @event;
    }
    
    public Transaction? GetTransaction(GraphQlDbContext dbContext) => 
        dbContext.Transactions.AsNoTracking().SingleOrDefault(t => t.Id == this.Event.TransactionId);
}
