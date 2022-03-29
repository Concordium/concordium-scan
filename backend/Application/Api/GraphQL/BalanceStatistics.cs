using HotChocolate;

namespace Application.Api.GraphQL;

public class BalanceStatistics
{
    public BalanceStatistics(ulong totalAmount, ulong? totalAmountReleased, ulong totalEncryptedAmount, ulong totalAmountLockedInReleaseSchedules, ulong bakingRewardAccount, ulong finalizationRewardAccount, ulong gasAccount)
    {
        TotalAmount = totalAmount;
        TotalAmountReleased = totalAmountReleased;
        TotalEncryptedAmount = totalEncryptedAmount;
        TotalAmountLockedInReleaseSchedules = totalAmountLockedInReleaseSchedules;
        BakingRewardAccount = bakingRewardAccount;
        FinalizationRewardAccount = finalizationRewardAccount;
        GasAccount = gasAccount;
    }

    [GraphQLDescription("The total CCD in existence")]
    public ulong TotalAmount { get; init; }
    
    [GraphQLDescription("The total CCD released according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule.")]
    public ulong? TotalAmountReleased { get; init; }

    [GraphQLDescription("The total CCD in encrypted balances")]
    public ulong TotalEncryptedAmount { get; init; }

    [GraphQLDescription("The total CCD locked in release schedules (from transfers with schedule)")]
    public ulong TotalAmountLockedInReleaseSchedules { get; set; }

    [GraphQLDescription("The amount in the baking reward account")]
    public ulong BakingRewardAccount { get; init; }

    [GraphQLDescription("The amount in the finalization reward account")]
    public ulong FinalizationRewardAccount { get; init; }

    [GraphQLDescription("The amount in the GAS account")]
    public ulong GasAccount { get; init; }
}