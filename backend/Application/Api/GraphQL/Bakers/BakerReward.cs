using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

public class BakerReward
{
    [GraphQLIgnore]
    public long BakerId { get; set; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; init; }
    
    public DateTimeOffset Timestamp { get; init; }

    public BakerRewardType RewardType { get; init; }

    public ulong Amount { get; init; }

    /// <summary>
    /// Reference to the block yielding the reward. 
    /// Not directly part of graphql schema but exposed indirectly through the reference field.
    /// </summary>
    [GraphQLIgnore]
    public long BlockId { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public async Task<Block> GetBlock([ScopedService] GraphQlDbContext dbContext)
    {
        return await dbContext.Blocks.AsNoTracking()
            .SingleAsync(x => x.Id == BlockId);
    }
}