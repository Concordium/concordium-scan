using HotChocolate;

namespace Application.Api.GraphQL;

public class ChainParameters
{
    [GraphQLIgnore]
    public int Id { get; init; }

    public decimal ElectionDifficulty { get; init; }
    
    public ExchangeRate EuroPerEnergy { get; init; }
    
    public ExchangeRate MicroCcdPerEuro { get; init; }
    
    public ulong BakerCooldownEpochs { get; init; }
    
    public int CredentialsPerBlockLimit { get; init; }
    
    public RewardParameters RewardParameters { get; init; }
    
    [GraphQLIgnore]
    public long FoundationAccountId { get; init; }
    
    public AccountAddress FoundationAccountAddress { get; init; }
    
    public ulong MinimumThresholdForBaking { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (ChainParameters)obj;
        return Id == other.Id &&
               ElectionDifficulty == other.ElectionDifficulty &&
               EuroPerEnergy.Equals(other.EuroPerEnergy) &&
               MicroCcdPerEuro.Equals(other.MicroCcdPerEuro) &&
               BakerCooldownEpochs == other.BakerCooldownEpochs &&
               CredentialsPerBlockLimit == other.CredentialsPerBlockLimit &&
               RewardParameters.Equals(other.RewardParameters) &&
               FoundationAccountId == other.FoundationAccountId &&
               MinimumThresholdForBaking == other.MinimumThresholdForBaking;
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
}

public class ExchangeRate
{
    public ulong Numerator { get; init; }
    public ulong Denominator { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (ExchangeRate)obj;
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    public static bool operator ==(ExchangeRate? left, ExchangeRate? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ExchangeRate? left, ExchangeRate? right)
    {
        return !Equals(left, right);
    }
}

public class RewardParameters
{
    public MintDistribution MintDistribution { get; init; }
    public TransactionFeeDistribution TransactionFeeDistribution { get; init; }
    public GasRewards GasRewards { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (RewardParameters)obj;
        return MintDistribution.Equals(other.MintDistribution) &&
               TransactionFeeDistribution.Equals(other.TransactionFeeDistribution) &&
               GasRewards.Equals(other.GasRewards);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MintDistribution, TransactionFeeDistribution, GasRewards);
    }

    public static bool operator ==(RewardParameters? left, RewardParameters? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RewardParameters? left, RewardParameters? right)
    {
        return !Equals(left, right);
    }
}

public class MintDistribution
{
    public decimal MintPerSlot { get; init; }
    public decimal BakingReward { get; init; }
    public decimal FinalizationReward { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (MintDistribution)obj;
        return MintPerSlot == other.MintPerSlot &&
               BakingReward == other.BakingReward &&
               FinalizationReward == other.FinalizationReward;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MintPerSlot, BakingReward, FinalizationReward);
    }

    public static bool operator ==(MintDistribution? left, MintDistribution? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MintDistribution? left, MintDistribution? right)
    {
        return !Equals(left, right);
    }
}

public class TransactionFeeDistribution
{
    public decimal Baker { get; init; }
    public decimal GasAccount { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (TransactionFeeDistribution)obj;
        return Baker == other.Baker
               && GasAccount == other.GasAccount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Baker, GasAccount);
    }

    public static bool operator ==(TransactionFeeDistribution? left, TransactionFeeDistribution? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TransactionFeeDistribution? left, TransactionFeeDistribution? right)
    {
        return !Equals(left, right);
    }
}

public class GasRewards
{
    public decimal Baker { get; init; }
    public decimal FinalizationProof { get; init; }
    public decimal AccountCreation { get; init; }
    public decimal ChainUpdate { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (GasRewards)obj;
        return Baker == other.Baker &&
               FinalizationProof == other.FinalizationProof &&
               AccountCreation == other.AccountCreation &&
               ChainUpdate == other.ChainUpdate;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Baker, FinalizationProof, AccountCreation, ChainUpdate);
    }

    public static bool operator ==(GasRewards? left, GasRewards? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(GasRewards? left, GasRewards? right)
    {
        return !Equals(left, right);
    }
}