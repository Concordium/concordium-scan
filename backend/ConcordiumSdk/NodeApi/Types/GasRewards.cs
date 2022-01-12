namespace ConcordiumSdk.NodeApi.Types;

public record GasRewards(
    decimal Baker,
    decimal FinalizationProof,
    decimal AccountCreation,
    decimal ChainUpdate);