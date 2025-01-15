using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Extensions;

namespace Application.Api.GraphQL;

public class ChainParametersV3 : ChainParameters, IEquatable<ChainParametersV3>
{
    public ulong RewardPeriodLength { get; init; }

    internal static ChainParametersV3 From(Concordium.Sdk.Types.ChainParametersV3 input)
    {
        return new ChainParametersV3
        {
            EuroPerEnergy = ExchangeRate.From(input.EuroPerEnergy),
            MicroCcdPerEuro = ExchangeRate.From(input.MicroCcdPerEuro),
            AccountCreationLimit = (int)input.AccountCreationLimit.Limit,
            FoundationAccountAddress = AccountAddress.From(input.FoundationAccount),
            RewardPeriodLength = input.TimeParameters.RewardPeriodLength.RewardPeriodEpochs.Count,
        };

    }

    public bool Equals(ChainParametersV3? other)
    {
        return other != null &&
               base.Equals(other) &&
               RewardPeriodLength == other.RewardPeriodLength;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as ChainParametersV2);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ChainParametersV3? left, ChainParametersV3? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParametersV3? left, ChainParametersV3? right)
    {
        return !Equals(left, right);
    }
}
