using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record ChainParametersV0(
    decimal ElectionDifficulty,
    ExchangeRate EuroPerEnergy,
    ExchangeRate MicroGTUPerEuro,
    ulong BakerCooldownEpochs,
    ushort AccountCreationLimit,
    RewardParametersV0 RewardParameters,
    ulong FoundationAccountIndex,
    CcdAmount MinimumThresholdForBaking);