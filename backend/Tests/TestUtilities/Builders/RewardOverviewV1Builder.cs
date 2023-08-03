using Application.Api.GraphQL.Extensions;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class RewardOverviewV1Builder
{
    private ProtocolVersion _protocolVersion = ProtocolVersion.P4;
    private readonly CcdAmount _totalAmount = CcdAmount.Zero;
    private readonly CcdAmount _totalEncryptedAmount = CcdAmount.Zero;
    private readonly CcdAmount _bakingRewardAccount = CcdAmount.Zero;
    private readonly CcdAmount _finalizationRewardAccount = CcdAmount.Zero;
    private readonly CcdAmount _gasAccount = CcdAmount.Zero;
    private readonly CcdAmount _foundationTransactionRewards = CcdAmount.Zero;
    private DateTimeOffset _nextPaydayTime = DateTimeOffset.MinValue;
    private MintRate _nextPaydayMintRate;
    private readonly CcdAmount _totalStakedCapital = CcdAmount.Zero;

    public RewardOverviewV1Builder()
    {
        var nextPaydayMintRate = MintRateExtensions.From(1);
        _nextPaydayMintRate = nextPaydayMintRate;
    }

    public RewardOverviewV1 Build()
    {
        return new RewardOverviewV1(
            _protocolVersion,
            _totalAmount,
            _totalEncryptedAmount,
            _bakingRewardAccount,
            _finalizationRewardAccount,
            _gasAccount,
            _foundationTransactionRewards,
            _nextPaydayTime,
            _nextPaydayMintRate,
            _totalStakedCapital
        );
    }

    public RewardOverviewV1Builder WithProtocolVersion(ProtocolVersion version)
    {
        _protocolVersion = version;
        return this;
    }

    public RewardOverviewV1Builder WithNextPaydayTime(DateTimeOffset time)
    {
        _nextPaydayTime = time;
        return this;
    }

    public RewardOverviewV1Builder WithNextPaydayMintRate(MintRate rate)
    {
        _nextPaydayMintRate = rate;
        return this;
    }
}