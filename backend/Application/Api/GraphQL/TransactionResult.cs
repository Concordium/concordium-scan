using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

[UnionType]
public abstract class TransactionResult { }

public class Success : TransactionResult
{
    private readonly Transaction _owner;

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(IncludeTotalCount = true)]
    public IEnumerable<TransactionResultEvent> GetEvents([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.TransactionResultEvents
            .AsNoTracking()
            .Where(x => x.TransactionId == _owner.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Entity);
    }
    
    public Success(Transaction owner) 
    {
        _owner = owner;
    }
}

public class Rejected : TransactionResult
{
    public TransactionRejectReason Reason { get; }
    
    public Rejected(TransactionRejectReason reason) 
    {
        Reason = reason;
    }
}
