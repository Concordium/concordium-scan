using Application.Api.GraphQL.Accounts;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Blocks;

[UnionType]
public abstract class SpecialEvent
{
    [GraphQLIgnore]
    public long BlockId { get; init; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; init; }
}

public class MintSpecialEvent : SpecialEvent
{
    public ulong BakingReward { get; init; }
    public ulong FinalizationReward { get; init; }
    public ulong PlatformDevelopmentCharge { get; init; }
    public AccountAddress FoundationAccountAddress  { get; init; }
}

public class FinalizationRewardsSpecialEvent : SpecialEvent
{
    public ulong Remainder { get; init; }

    /// <summary>
    /// The AccountAddresses and Amounts are stored in two arrays instead of a single array of address/amount pairs
    /// due to the way they are stored in the database and the ability of EF-core to handle that.
    ///
    /// GraphQL schema-wise the correct structure is created in <see cref="GetFinalizationRewards"/>
    /// </summary>
    [GraphQLIgnore] 
    public AccountAddress[] AccountAddresses { get; init; }
        
    /// <summary>
    /// See comment on <see cref="AccountAddresses"/>
    /// </summary>
    [GraphQLIgnore]
    public ulong[] Amounts { get; init; }
    
    [UsePaging(InferConnectionNameFromField = false)]
    public IEnumerable<AccountAddressAmount> GetFinalizationRewards()
    {
        if (AccountAddresses.Length != Amounts.Length) throw new InvalidOperationException("The array lengths do not match");
        
        for (int i = 0; i < AccountAddresses.Length; i++)
            yield return new AccountAddressAmount(AccountAddresses[i], Amounts[i]);
    }
}

public class BlockRewardsSpecialEvent : SpecialEvent
{
    public ulong TransactionFees { get; init; }
    public ulong OldGasAccount { get; init; }
    public ulong NewGasAccount { get; init; }
    public ulong BakerReward { get; init; }
    public ulong FoundationCharge { get; init; }
    public AccountAddress BakerAccountAddress { get; init; }
    public AccountAddress FoundationAccountAddress { get; init; }
}

public class BakingRewardsSpecialEvent : SpecialEvent
{
    public ulong Remainder { get; init; }
    
    /// <summary>
    /// The AccountAddresses and Amounts are stored in two arrays instead of a single array of address/amount pairs
    /// due to the way they are stored in the database and the ability of EF-core to handle that.
    ///
    /// GraphQL schema-wise the correct structure is created in <see cref="GetBakingRewards"/>
    /// </summary>
    [GraphQLIgnore] 
    public AccountAddress[] AccountAddresses { get; init; }
    
    /// <summary>
    /// See comment on <see cref="AccountAddresses"/>
    /// </summary>
    [GraphQLIgnore]
    public ulong[] Amounts { get; init; }

    [UsePaging(InferConnectionNameFromField = false)]
    public IEnumerable<AccountAddressAmount> GetBakingRewards()
    {
        if (AccountAddresses.Length != Amounts.Length) throw new InvalidOperationException("The array lengths do not match");
        
        for (int i = 0; i < AccountAddresses.Length; i++)
            yield return new AccountAddressAmount(AccountAddresses[i], Amounts[i]);
    }
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
    
    [GraphQLDescription("The amount awarded to the passive delegators.")]
    public ulong PassiveReward { get; init; }
    
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
    [GraphQLDescription("The pool awarded.")]
    public PoolRewardTarget Pool { get; init; }

    [GraphQLDescription("Accrued transaction fees for pool.")]
    public ulong TransactionFees { get; init; }
    
    [GraphQLDescription("Accrued baking rewards for pool.")]
    public ulong BakerReward { get; init; }
    
    [GraphQLDescription("Accrued finalization rewards for pool.")]
    public ulong FinalizationReward { get; init; }
}