using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

public class PaydayPoolReward
{
    [GraphQLIgnore]
    public PoolRewardTarget Pool { get; set; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; init; }
    
    public DateTimeOffset Timestamp { get; init; }
    
    [GraphQLIgnore]
    public ulong TransactionFeesTotalAmount { get; init; }
    [GraphQLIgnore]
    public ulong TransactionFeesBakerAmount { get; init; }
    [GraphQLIgnore]
    public ulong TransactionFeesDelegatorsAmount { get; init; }

    public PaydayPoolRewardAmounts TransactionFees => new(TransactionFeesTotalAmount, TransactionFeesBakerAmount, TransactionFeesDelegatorsAmount);
    
    [GraphQLIgnore]
    public ulong BakerRewardTotalAmount { get; init; }
    [GraphQLIgnore]
    public ulong BakerRewardBakerAmount { get; init; }
    [GraphQLIgnore]
    public ulong BakerRewardDelegatorsAmount { get; init; }

    public PaydayPoolRewardAmounts BakerReward => new(BakerRewardTotalAmount, BakerRewardBakerAmount, BakerRewardDelegatorsAmount);

    [GraphQLIgnore]
    public ulong FinalizationRewardTotalAmount { get; init; }
    [GraphQLIgnore]
    public ulong FinalizationRewardBakerAmount { get; init; }
    [GraphQLIgnore]
    public ulong FinalizationRewardDelegatorsAmount { get; init; }

    public PaydayPoolRewardAmounts FinalizationReward => new(FinalizationRewardTotalAmount, FinalizationRewardBakerAmount, FinalizationRewardDelegatorsAmount);

    [GraphQLIgnore]
    public ulong SumTotalAmount { get; init; }
    [GraphQLIgnore]
    public ulong SumBakerAmount { get; init; }
    [GraphQLIgnore]
    public ulong SumDelegatorsAmount { get; init; }

    [GraphQLIgnore]
    public ulong PaydayDurationSeconds { get; init; }
    
    [GraphQLIgnore]
    public double? TotalApy { get; init; }
    [GraphQLIgnore]
    public double? BakerApy { get; init; }
    [GraphQLIgnore]
    public double? DelegatorsApy { get; init; }

    [GraphQLDescription("The sum of the transaction fees, baker rewards and finalization rewards.")]
    public PaydayPoolRewardAmounts Sum => new(SumTotalAmount, SumBakerAmount, SumDelegatorsAmount);

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

public record PaydayPoolRewardAmounts(
    [property:GraphQLDescription("The total amount (baker + delegators)")]
    ulong TotalAmount, 
    [property:GraphQLDescription("The bakers share of the reward")]
    ulong BakerAmount, 
    [property:GraphQLDescription("The delegators share of the reward")]
    ulong DelegatorsAmount);