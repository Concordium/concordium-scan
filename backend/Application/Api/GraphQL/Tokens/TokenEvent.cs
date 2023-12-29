using Application.Api.GraphQL.EfCore;
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
    public long TransactionId { get; set; }
    public CisEventData Event { get; set; }

    public TokenEvent(
        ulong contractIndex,
        ulong contractSubIndex,
        string tokenId,
        long transactionId,
        CisEventData @event)
    {
        ContractIndex = contractIndex;
        ContractSubIndex = contractSubIndex;
        TokenId = tokenId;
        TransactionId = transactionId;
        Event = @event;
    }
    
    public Transaction? GetTransaction(GraphQlDbContext dbContext) => 
        dbContext.Transactions.AsNoTracking().SingleOrDefault(t => t.Id == this.TransactionId);
}
