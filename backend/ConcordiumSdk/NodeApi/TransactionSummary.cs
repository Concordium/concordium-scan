namespace ConcordiumSdk.NodeApi;

public class TransactionSummary
{
    public string Sender { get; init; } // AccountAddress: ???
    public string Hash { get; init; } // TransactionHash: Same as BlockHash https://github.com/Concordium/concordium-base/blob/61bf91d568be33dfbf92d4d18e679ec625e25582/haskell-src/Concordium/Types.hs line 792
    public string Cost { get; init; } // Amount: (HS: Word64 = C#: ulong) https://github.com/Concordium/concordium-base/blob/61bf91d568be33dfbf92d4d18e679ec625e25582/haskell-src/Concordium/Common/Amount.hs 
    public int EnergyCost { get; init; }  // Energy (HS: Word64 = C#: ulong) https://github.com/Concordium/concordium-base/blob/61bf91d568be33dfbf92d4d18e679ec625e25582/haskell-src/Concordium/Types.hs line 568
    public TransactionSummaryType Type { get; init; }
    public TransactionResult Result { get; init; }
    public int Index { get; init; } // TransactionIndex (HS: Word64 = C#: ulong) https://github.com/Concordium/concordium-base/blob/a50612e023da79cb625cd36c52703af6ed483738/haskell-src/Concordium/Types/Execution.hs line 947
}

/// <summary>
/// Consider if we should model this more type safe with enums and polymorphism -
/// Just not sure how that would play nicely with API's (REST, GraphQL, ...)
/// </summary>
public class TransactionSummaryType
{
    /// <summary>
    /// accountTransaction, credentialDeploymentTransaction, updateTransaction (src: https://github.com/Concordium/concordium-base/blob/a50612e023da79cb625cd36c52703af6ed483738/haskell-src/Concordium/Types/Execution.hs line 959)
    /// </summary>
    public string Type { get; init; }
    
    /// <summary>
    /// Depends on type
    /// </summary>
    public string Contents { get; init; }
}

public enum TransactionType
{
    AccountTransactionType, 
    CredentialDeploymentTransactionType,
    UpdateTransactionType
}

public class TransactionResult
{
    public string Outcome { get; init; }
    public TransactionResultEvent[] Events { get; init; }
}

public class TransactionResultEvent
{
}

public class Transferred : TransactionResultEvent
{
    public string Amount { get; init; }
    public AddressWithType To { get; init; }
    public AddressWithType From { get; init; }
}

public class AddressWithType
{
    public string Address { get; init; }
    public string Type { get; init; }
}