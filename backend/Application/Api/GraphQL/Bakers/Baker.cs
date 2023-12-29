using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

public class Baker
{
    [ID]
    public long Id { get; set; }
    
    public long BakerId => Id;
    
    public BakerState State
    {
        get;
        set;
    }

    /// <summary>
    /// This property is there for loading the statistics row from the database. The data
    /// is exposed elsewhere in the baker model to create a better and more meaningful model.
    /// </summary>
    [GraphQLIgnore] 
    public BakerStatisticsRow? Statistics { get; private set; }
    
    /// <summary>
    /// This property is there for loading the pool apys row from the database. The data
    /// is exposed elsewhere in the baker model to create a better and more meaningful model.
    /// </summary>
    [GraphQLIgnore] 
    public PoolApys? PoolApys { get; set; }

    /// <summary>
    /// DONT USE THIS PROPERTY!
    /// 
    /// EF-core 6 does not support inheritance hierarchies for owned entities.
    ///
    /// Since this is the ideal model for active/removed bakers, this is the 
    /// work-around to let EF-core read/write the state as a polymorphic instance. 
    /// 
    /// Issue is tracked here https://github.com/dotnet/efcore/issues/9630
    /// </summary>
    [GraphQLIgnore] 
    public ActiveBakerState? ActiveState
    {
        get => State as ActiveBakerState;
        set => State = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// See comment on ActiveState property
    /// </summary>
    [GraphQLIgnore] 
    public RemovedBakerState? RemovedState
    {
        get => State as RemovedBakerState;
        set => State = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Task<Account> GetAccount(GraphQlDbContext dbContext)
    {
        // Account and baker share the same ID!
        return dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(x => x.Id == Id);
    }
    
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "baker_transaction_relation_by_descending_index")] 
    [GraphQLDescription("Get the transactions that have affected the baker.")]
    public IQueryable<BakerTransactionRelation> GetTransactions(GraphQlDbContext dbContext)
    {
        return dbContext.BakerTransactionRelations
            .AsNoTracking()
            .Where(x => x.BakerId == Id)
            .OrderByDescending(x => x.Index);
    }
    
    internal static Baker CreateNewBaker(BakerId bakerId, CcdAmount stakedAmount, bool shouldRestakeEarnings, BakerPool? pool)
    {
        return new Baker
        {
            Id = (long)bakerId.Id.Index,
            State = new ActiveBakerState(stakedAmount.Value, shouldRestakeEarnings, pool, null)
        };
    }
    
    internal BakerPool GetPool()
    {
        var activeState = State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set open status for a baker that is not active!");
        return activeState.Pool ?? throw new InvalidOperationException("Cannot set open status for a baker where pool is null!");
    }
}
