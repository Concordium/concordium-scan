using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Accounts;

public class AccountReward
{
    [GraphQLIgnore]
    public long AccountId { get; set; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; init; }
    
    public DateTimeOffset Timestamp { get; init; }

    public RewardType RewardType { get; init; }

    public ulong Amount { get; init; }

    /// <summary>
    /// Reference to the block yielding the reward. 
    /// Not directly part of graphql schema but exposed indirectly through the reference field.
    /// </summary>
    [GraphQLIgnore]
    public long BlockId { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Task<Block> GetBlock([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks.AsNoTracking()
            .SingleAsync(x => x.Id == BlockId);
    }
}