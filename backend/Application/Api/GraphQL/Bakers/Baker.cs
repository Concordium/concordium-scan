using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

public class Baker
{
    [ID]
    public long Id { get; set; }
    
    public long BakerId => Id;
    
    [GraphQLIgnore] // See comment on GetState()
    public BakerStatus Status { get; set; }
    
    [GraphQLIgnore] // See comment on GetState()
    public PendingBakerChange? PendingChange { get; set; }

    /// <summary>
    /// EF-core 6 does not support inheritance hierarchies for owned entities.
    ///
    /// Since that would have been the ideal internal model for active/removed bakers, this is the 
    /// work-around to expose this model in the GraphQL schema. When EF-core supports it we can changed the internal
    /// model.
    /// 
    /// Issue is tracked here https://github.com/dotnet/efcore/issues/9630
    /// </summary>
    public BakerState GetState()
    {
        return Status switch
        {
            BakerStatus.Active => new ActiveBakerState(PendingChange),
            BakerStatus.Removed => new RemovedBakerState(),
            _ => throw new NotImplementedException()
        };
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Task<Account> GetAccount([ScopedService] GraphQlDbContext dbContext)
    {
        // Account and baker share the same ID!
        return dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(x => x.Id == Id);
    }

    public void SetState(BakerState state)
    {
        if (state is ActiveBakerState activeState)
        {
            Status = BakerStatus.Active;
            PendingChange = activeState.PendingChange;
        }
        else if (state is RemovedBakerState removedState)
        {
            Status = BakerStatus.Removed;
            PendingChange = null;
        }
    }
}
