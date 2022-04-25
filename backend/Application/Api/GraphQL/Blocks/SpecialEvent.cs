using Application.Api.GraphQL.Accounts;
using HotChocolate;

namespace Application.Api.GraphQL.Blocks;

public abstract class SpecialEvent
{
    [GraphQLIgnore]
    public long BlockId { get; init; }

    [GraphQLIgnore]
    public long Index { get; init; }
}

public class PaydayAccountRewardSpecialEvent : SpecialEvent
{
    [GraphQLDescription("The account that got rewarded.")]
    public AccountAddress Account { get; init; }
    
    [GraphQLDescription("The transaction fee reward at payday to the account.")]
    public ulong TransactionFees { get; init; }
    
    [GraphQLDescription("The baking reward at payday to the account.")]
    public ulong BakerReward { get; init; }
    
    [GraphQLDescription("The finalization reward at payday to the account.")]
    public ulong FinalizationReward { get; init; }
}

public class BlockAccrueRewardSpecialEvent : SpecialEvent
{
    [GraphQLDescription("The total fees paid for transactions in the block.")]
    public ulong TransactionFees { get; init; }
    
    [GraphQLDescription("The old balance of the GAS account.")]
    public ulong OldGasAccount { get; init; }
    
    [GraphQLDescription("The new balance of the GAS account.")]
    public ulong NewGasAccount { get; init; }
    
    [GraphQLDescription("The amount awarded to the baker.")]
    public ulong BakerReward { get; init; }
    
    [GraphQLDescription("The amount awarded to the L-Pool.")]
    public ulong LPoolReward { get; init; }
    
    [GraphQLDescription("The amount awarded to the foundation.")]
    public ulong FoundationCharge { get; init; }
    
    [GraphQLDescription("The baker of the block, who will receive the award.")]
    public ulong BakerId { get; init; }
}

public class PaydayFoundationRewardSpecialEvent : SpecialEvent
{
    public AccountAddress FoundationAccount { get; init; }
    public ulong DevelopmentCharge { get; init; }
}

public class PaydayPoolRewardSpecialEvent : SpecialEvent
{
    [GraphQLDescription("The pool owner (L-Pool when null).")]
    public ulong? PoolOwner { get; init; }
    
    [GraphQLDescription("Accrued transaction fees for pool.")]
    public ulong TransactionFees { get; init; }
    
    [GraphQLDescription("Accrued baking rewards for pool.")]
    public ulong BakerReward { get; init; }
    
    [GraphQLDescription("Accrued finalization rewards for pool.")]
    public ulong FinalizationReward { get; init; }
}