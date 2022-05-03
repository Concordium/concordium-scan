using Application.Api.GraphQL.Accounts;
using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

[InterfaceType]
public abstract class ChainParameters
{
    [GraphQLIgnore]
    public int Id { get; init; }
    
    public decimal ElectionDifficulty { get; init; }
    
    public ExchangeRate EuroPerEnergy { get; init; }
    
    public ExchangeRate MicroCcdPerEuro { get; init; }

    public int AccountCreationLimit { get; init; }
    
    [GraphQLIgnore]
    public long FoundationAccountId { get; init; }
    
    public AccountAddress FoundationAccountAddress { get; init; }
    
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
               AccountCreationLimit == other.AccountCreationLimit &&
               FoundationAccountId == other.FoundationAccountId &&
               FoundationAccountAddress == other.FoundationAccountAddress;
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