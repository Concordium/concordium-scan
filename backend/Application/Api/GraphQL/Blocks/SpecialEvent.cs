using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

namespace Application.Api.GraphQL.Blocks;

[UnionType]
public abstract class SpecialEvent
{
    [GraphQLIgnore]
    public long BlockId { get; init; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; init; }
    
    internal static IEnumerable<SpecialEvent> MapSpecialEvents(long blockId, IList<ISpecialEvent> inputs)
    {
        foreach (var input in inputs)
        {
            switch (input)
            {
                case BakingRewards bakingRewards:
                    var bakingRewardsAccountAmounts = bakingRewards.Rewards
                        .Select(kv => (AccountAddress: kv.Key, Amount: kv.Value))
                        .ToList();
                    yield return new BakingRewardsSpecialEvent
                    {
                        BlockId = blockId,
                        Remainder = bakingRewards.Remainder.Value, 
                        AccountAddresses = bakingRewardsAccountAmounts
                            .Select(reward => new AccountAddress(reward.AccountAddress.ToString())).ToArray(),
                        Amounts = bakingRewardsAccountAmounts.Select(reward => reward.Amount.Value).ToArray()
                    };
                    break;
                case BlockAccrueReward blockAccrueReward:
                    yield return new BlockAccrueRewardSpecialEvent
                    {
                        BlockId = blockId,
                        TransactionFees = blockAccrueReward.TransactionFees.Value,
                        OldGasAccount = blockAccrueReward.OldGasAccount.Value,
                        NewGasAccount = blockAccrueReward.NewGasAccount.Value,
                        BakerReward = blockAccrueReward.BakerReward.Value,
                        PassiveReward = blockAccrueReward.PassiveReward.Value,
                        FoundationCharge = blockAccrueReward.FoundationCharge.Value,
                        BakerId = blockAccrueReward.BakerId.Id.Index
                    };
                    break;
                case BlockReward blockReward:
                    yield return new BlockRewardsSpecialEvent
                    {
                        BlockId = blockId,
                        TransactionFees = blockReward.TransactionFees.Value,
                        OldGasAccount = blockReward.OldGasAccount.Value,
                        NewGasAccount = blockReward.NewGasAccount.Value,
                        BakerReward = blockReward.BakerReward.Value,
                        FoundationCharge = blockReward.FoundationCharge.Value,
                        BakerAccountAddress = new AccountAddress(blockReward.Baker.ToString()),
                        FoundationAccountAddress = new AccountAddress(blockReward.FoundationAccount.ToString())
                    };
                    break;
                case FinalizationRewards finalizationRewards:
                    var accountAmounts = finalizationRewards.Rewards
                        .Select(kv => (AccountAddress: kv.Key, Amount: kv.Value))
                        .ToList();
                    yield return new FinalizationRewardsSpecialEvent
                    {
                        BlockId = blockId,
                        Remainder = finalizationRewards.Remainder.Value,
                        AccountAddresses = accountAmounts
                            .Select(reward => new AccountAddress(reward.AccountAddress.ToString())).ToArray(),
                        Amounts = accountAmounts.Select(reward => reward.Amount.Value).ToArray()
                    };
                    break;
                case Mint mint:
                    yield return new MintSpecialEvent
                    {
                        BlockId = blockId,
                        BakingReward = mint.MintBakingReward.Value,
                        FinalizationReward = mint.MintFinalizationReward.Value,
                        PlatformDevelopmentCharge = mint.MintPlatformDevelopmentCharge.Value,
                        FoundationAccountAddress = new AccountAddress(mint.FoundationAccount.ToString())
                    };
                    break;
                case PaydayAccountReward paydayAccountReward:
                    yield return new PaydayAccountRewardSpecialEvent
                    {
                        BlockId = blockId,
                        Account = new AccountAddress(paydayAccountReward.Account.ToString()),
                        TransactionFees = paydayAccountReward.TransactionFees.Value,
                        BakerReward = paydayAccountReward.BakerReward.Value,
                        FinalizationReward = paydayAccountReward.FinalizationReward.Value
                    };
                    break;
                case PaydayFoundationReward paydayFoundationReward:
                    yield return new PaydayFoundationRewardSpecialEvent
                    {
                        BlockId = blockId,
                        FoundationAccount = new AccountAddress(paydayFoundationReward.FoundationAccount.ToString()),
                        DevelopmentCharge = paydayFoundationReward.DevelopmentCharge.Value,
                    };
                    break;
                case PaydayPoolReward paydayPoolReward:
                    yield return new PaydayPoolRewardSpecialEvent
                    {
                        BlockId = blockId,
                        Pool = paydayPoolReward.PoolOwner.HasValue
                            ? new BakerPoolRewardTarget((long)paydayPoolReward.PoolOwner.Value)
                            : new PassiveDelegationPoolRewardTarget(),
                        TransactionFees = paydayPoolReward.TransactionFees.Value,
                        BakerReward = paydayPoolReward.BakerReward.Value,
                        FinalizationReward = paydayPoolReward.FinalizationReward.Value
                    };
                    break;
            }
        }
    }
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