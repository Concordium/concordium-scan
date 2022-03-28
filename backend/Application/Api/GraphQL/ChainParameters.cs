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