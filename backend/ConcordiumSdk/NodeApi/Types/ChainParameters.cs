using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record ChainParameters(
    decimal ElectionDifficulty,
    ExchangeRate EuroPerEnergy,
    ExchangeRate MicroGTUPerEuro,
    ulong BakerCooldownEpochs,
    ushort CredentialsPerBlockLimit,
    RewardParameters RewardParameters,
    ulong FoundationAccountIndex,
    CcdAmount MinimumThresholdForBaking);