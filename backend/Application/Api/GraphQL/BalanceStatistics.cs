using HotChocolate;

namespace Application.Api.GraphQL;

public record BalanceStatistics(
    [property:GraphQLDescription("The total CCD in existence")]
    ulong TotalAmount,
    [property:GraphQLDescription("The total CCD in encrypted balances")]
    ulong TotalEncryptedAmount,
    [property:GraphQLDescription("The total CCD locked in release schedules (from transfers with schedule)")]
    ulong TotalAmountLockedInReleaseSchedules,
    [property:GraphQLDescription("The amount in the baking reward account")]
    ulong BakingRewardAccount,
    [property:GraphQLDescription("The amount in the finalization reward account")]
    ulong FinalizationRewardAccount,
    [property:GraphQLDescription("The amount in the GAS account")]
    ulong GasAccount);