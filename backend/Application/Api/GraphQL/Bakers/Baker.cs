using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
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

    [UseDbContext(typeof(GraphQlDbContext))]
    public Task<Account> GetAccount([ScopedService] GraphQlDbContext dbContext)
    {
        // Account and baker share the same ID!
        return dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(x => x.Id == Id);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "baker_transaction_relation_by_descending_index")] 
    [GraphQLDescription("Get the transactions that have affected the baker.")]
    public IQueryable<BakerTransactionRelation> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.BakerTransactionRelations
            .AsNoTracking()
            .Where(x => x.BakerId == Id)
            .OrderByDescending(x => x.Index);
    }
}
