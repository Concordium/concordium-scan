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
    
    [GraphQLIgnore] // Included in the TransactionFees aggregated type
    public ulong TransactionFeesTotalAmount { get; init; }
    [GraphQLIgnore] // Included in the TransactionFees aggregated type
    public ulong TransactionFeesBakerAmount { get; init; }
    [GraphQLIgnore] // Included in the TransactionFees aggregated type
    public ulong TransactionFeesDelegatorsAmount { get; init; }

    public PaydayPoolRewardAmounts TransactionFees => new(TransactionFeesTotalAmount, TransactionFeesBakerAmount, TransactionFeesDelegatorsAmount);
    
    [GraphQLIgnore] // Included in the BakerReward aggregated type
    public ulong BakerRewardTotalAmount { get; init; }
    [GraphQLIgnore] // Included in the BakerReward aggregated type
    public ulong BakerRewardBakerAmount { get; init; }
    [GraphQLIgnore] // Included in the BakerReward aggregated type
    public ulong BakerRewardDelegatorsAmount { get; init; }

    public PaydayPoolRewardAmounts BakerReward => new(BakerRewardTotalAmount, BakerRewardBakerAmount, BakerRewardDelegatorsAmount);

    [GraphQLIgnore] // Included in the FinalizationReward aggregated type
    public ulong FinalizationRewardTotalAmount { get; init; }
    [GraphQLIgnore] // Included in the FinalizationReward aggregated type
    public ulong FinalizationRewardBakerAmount { get; init; }
    [GraphQLIgnore] // Included in the FinalizationReward aggregated type
    public ulong FinalizationRewardDelegatorsAmount { get; init; }

    public PaydayPoolRewardAmounts FinalizationReward => new(FinalizationRewardTotalAmount, FinalizationRewardBakerAmount, FinalizationRewardDelegatorsAmount);

    [GraphQLIgnore] // Included in the Sum aggregated type
    public ulong SumTotalAmount { get; init; }
    [GraphQLIgnore] // Included in the Sum aggregated type
    public ulong SumBakerAmount { get; init; }
    [GraphQLIgnore] // Included in the Sum aggregated type
    public ulong SumDelegatorsAmount { get; init; }

    [GraphQLDescription("The sum of the transaction fees, baker rewards and finalization rewards.")]
    public PaydayPoolRewardAmounts Sum => new(SumTotalAmount, SumBakerAmount, SumDelegatorsAmount);

    [GraphQLIgnore]
    public ulong PaydayDurationSeconds { get; init; }
    
    [GraphQLDescription("The APY calculated for this single reward taking into consideration the combined reward and stake of baker and delegators.")]
    public double? TotalApy { get; init; }
    [GraphQLDescription("The APY calculated for this single reward taking into consideration only the bakers reward and stake. Will be null if there was no baker stake (passive delegation).")]
    public double? BakerApy { get; init; }
    [GraphQLDescription("The APY calculated for this single reward taking into consideration only the delegators reward and stake. Will be null if there was no delegated stake.")]
    public double? DelegatorsApy { get; init; }

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