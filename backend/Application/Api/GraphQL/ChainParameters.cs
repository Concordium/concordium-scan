using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

namespace Application.Api.GraphQL;

[InterfaceType]
public abstract class ChainParameters : IEquatable<ChainParameters>
{
    [GraphQLIgnore]
    public int Id { get; init; }

    public ExchangeRate EuroPerEnergy { get; init; }
    
    public ExchangeRate MicroCcdPerEuro { get; init; }

    public int AccountCreationLimit { get; init; }

    public AccountAddress FoundationAccountAddress { get; init; }

    public bool Equals(ChainParameters? other)
    {
        return
            other != null &&
            GetType() == other.GetType() &&
            Id == other.Id &&
            EuroPerEnergy.Equals(other.EuroPerEnergy) &&
            MicroCcdPerEuro.Equals(other.MicroCcdPerEuro) &&
            AccountCreationLimit == other.AccountCreationLimit &&
            FoundationAccountAddress == other.FoundationAccountAddress;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as ChainParameters);
    }

    public override int GetHashCode()
    {
        return Id;
    }
    
    public static bool operator ==(ChainParameters? left, ChainParameters? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParameters? left, ChainParameters? right)
    {
        return !Equals(left, right);
    }
    
    internal static ChainParameters From(IChainParameters chainParameters, int id = default)
    {
        return chainParameters switch
        {
            Concordium.Sdk.Types.ChainParametersV0 chainParametersV0 => ChainParametersV0.From(chainParametersV0, id),
            Concordium.Sdk.Types.ChainParametersV1 chainParametersV1 => ChainParametersV1.From(chainParametersV1, id),
            Concordium.Sdk.Types.ChainParametersV2 chainParametersV2 => ChainParametersV2.From(chainParametersV2, id),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Present from protocol version 4 and above and hence from chain parameters 1 and above.
    /// </summary>
    internal static bool TryGetPoolOwnerCooldown(ChainParameters chainParameters, out ulong? poolOwnerCooldown)
    {
        switch (chainParameters)
        {
            case ChainParametersV0:
                poolOwnerCooldown = null;
                return false;
            case ChainParametersV1 chainParametersV1:
                poolOwnerCooldown = chainParametersV1.PoolOwnerCooldown;
                return true;
            case ChainParametersV2 chainParametersV2:
                poolOwnerCooldown = chainParametersV2.PoolOwnerCooldown;
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(chainParameters));
        }
    }

    /// <summary>
    /// Present from protocol version 4 and above and hence from chain parameters 1 and above.
    /// </summary>
    internal static bool TryGetCommissionRanges(
        ChainParameters chainParameters,
        out CommissionRange? finalizationCommissionRange,
        out CommissionRange? bakingCommissionRange,
        out CommissionRange? transactionCommissionRange)
    {
        switch (chainParameters)
        {
            case ChainParametersV0:
                finalizationCommissionRange = null;
                bakingCommissionRange = null;
                transactionCommissionRange = null;
                return false;
            case ChainParametersV1 chainParametersV1:
                finalizationCommissionRange = chainParametersV1.FinalizationCommissionRange;
                bakingCommissionRange = chainParametersV1.BakingCommissionRange;
                transactionCommissionRange = chainParametersV1.TransactionCommissionRange;
                return true;
            case ChainParametersV2 chainParametersV2:
                finalizationCommissionRange = chainParametersV2.FinalizationCommissionRange;
                bakingCommissionRange = chainParametersV2.BakingCommissionRange;
                transactionCommissionRange = chainParametersV2.TransactionCommissionRange;
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(chainParameters));
        }
    }
    
    /// <summary>
    /// Present from protocol version 4 and above and hence from chain parameters 1 and above.
    /// </summary>
    internal static bool TryGetDelegatorCooldown(ChainParameters chainParameters,
        out ulong? delegatorCooldown)
    {
        switch (chainParameters)
        {
            case ChainParametersV0:
                delegatorCooldown = null;
                return false;
            case ChainParametersV1 chainParametersV1:
                delegatorCooldown = chainParametersV1.DelegatorCooldown;
                return true;
            case ChainParametersV2 chainParametersV2:
                delegatorCooldown = chainParametersV2.DelegatorCooldown;
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(chainParameters));
        }
    }
    
    /// <summary>
    /// Present from protocol version 4 and above and hence from chain parameters 1 and above.
    /// </summary>
    internal static bool TryGetCapitalBoundAndLeverageFactor(
        ChainParameters chainParameters,
        out decimal? capitalBound,
        out LeverageFactor? leverageFactor)
    {
        switch (chainParameters)
        {
            case ChainParametersV0:
                capitalBound = null;
                leverageFactor = null;
                return false;
            case ChainParametersV1 chainParametersV1:
                capitalBound = chainParametersV1.CapitalBound;
                leverageFactor = chainParametersV1.LeverageBound;
                return true;
            case ChainParametersV2 chainParametersV2:
                capitalBound = chainParametersV2.CapitalBound;
                leverageFactor = chainParametersV2.LeverageBound;
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(chainParameters));
        }
    }
    
    
}