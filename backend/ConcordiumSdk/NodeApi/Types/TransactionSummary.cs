using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class TransactionSummary
{
    public TransactionSummary(AccountAddress? sender, TransactionHash hash, CcdAmount cost, int energyCost, TransactionType type, TransactionResult result, int index)
    {
        Sender = sender;
        Hash = hash;
        Cost = cost;
        EnergyCost = energyCost;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        Index = index;
    }

    public AccountAddress? Sender { get;  }
    public TransactionHash Hash { get; } 
    public CcdAmount Cost { get; }  
    public int EnergyCost { get; }  
    public TransactionType Type { get; }
    public TransactionResult Result { get; }
    
    /// <summary>
    /// The index of the transaction in the block (0 based).
    /// </summary>
    public int Index { get; } 
}

