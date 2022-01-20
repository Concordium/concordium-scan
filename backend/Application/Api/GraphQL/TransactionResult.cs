using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

[InterfaceType("TransactionResult")]
public abstract class TransactionResult
{
    protected TransactionResult(bool successful)
    {
        Successful = successful;
    }

    public bool Successful { get; }
}

public class Successful : TransactionResult
{
    private readonly Transaction _owner;

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IEnumerable<TransactionResultEvent> GetEvents([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.TransactionResultEvents
            .AsNoTracking()
            .Where(x => x.TransactionId == _owner.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Entity);
    }
    
    public Successful(Transaction owner) : base(true)
    {
        _owner = owner;
    }
}

public class Rejected : TransactionResult
{
    public TransactionRejectReason Reason { get; }
    
    public Rejected(TransactionRejectReason reason) : base(false)
    {
        Reason = reason;
    }
}
