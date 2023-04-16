using HotChocolate;

namespace Application.Api.GraphQL.Blocks;

public class BalanceStatistics
{
    public BalanceStatistics(
        ulong totalAmount, 
        ulong? totalAmountReleased, 
        ulong? totalAmountUnlocked, 
        ulong totalAmountEncrypted, 
        ulong totalAmountLockedInReleaseSchedules, 
        ulong totalAmountStaked, 
        ulong totalAmountStakedByBakers, 
        ulong totalAmountStakedByDelegation, 
        ulong bakingRewardAccount, 
        ulong finalizationRewardAccount, 
        ulong gasAccount)
    {
        TotalAmount = totalAmount;
        TotalAmountReleased = totalAmountReleased;
        TotalAmountUnlocked = totalAmountUnlocked;
        TotalAmountEncrypted = totalAmountEncrypted;
        TotalAmountLockedInReleaseSchedules = totalAmountLockedInReleaseSchedules;
        TotalAmountStaked = totalAmountStaked;
        TotalAmountStakedByBakers = totalAmountStakedByBakers;
        TotalAmountStakedByDelegation = totalAmountStakedByDelegation;
        BakingRewardAccount = bakingRewardAccount;
        FinalizationRewardAccount = finalizationRewardAccount;
        GasAccount = gasAccount;
    }

    [GraphQLDescription("The total CCD in existence")]
    public ulong TotalAmount { get; init; }
    
    [GraphQLDescription("The total CCD Released. This is total CCD supply not counting the balances of non circulating accounts")]
    public ulong? TotalAmountReleased { get; init; }

    [GraphQLDescription("The total CCD Unlocked according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule.")]
    public ulong? TotalAmountUnlocked { get; init; }

    [GraphQLDescription("The total CCD in encrypted balances")]
    public ulong TotalAmountEncrypted { get; init; }

    [GraphQLDescription("The total CCD locked in release schedules (from transfers with schedule)")]
    public ulong TotalAmountLockedInReleaseSchedules { get; set; }
    
    [GraphQLDescription("The total CCD staked")]
    public ulong TotalAmountStaked { get; set; }
    
    [GraphQLIgnore] // Still not part of graphql schema... open once staking and delegation is live
    [GraphQLDescription("The total CCD staked by bakers")]
    public ulong TotalAmountStakedByBakers { get; set; }

    [GraphQLIgnore] // Still not part of graphql schema... open once staking and delegation is live
    [GraphQLDescription("The total CCD staked by accounts that have delegated stake to a baker pool or the passive pool")]
    public ulong TotalAmountStakedByDelegation { get; set; }
    
    [GraphQLDescription("The amount in the baking reward account")]
    public ulong BakingRewardAccount { get; init; }

    [GraphQLDescription("The amount in the finalization reward account")]
    public ulong FinalizationRewardAccount { get; init; }

    [GraphQLDescription("The amount in the GAS account")]
    public ulong GasAccount { get; init; }
}
