using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

public class NextAccountNonceResponse
{
    public NextAccountNonceResponse(Nonce nonce, bool allFinal)
    {
        Nonce = nonce;
        AllFinal = allFinal;
    }

    /// <summary>
    /// Next account nonce.
    /// </summary>
    public Nonce Nonce { get; }
    
    /// <summary>
    /// Indicates if all account transactions are finalized. 
    /// </summary>
    public bool AllFinal { get; }
}