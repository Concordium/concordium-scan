namespace Application.Import.ConcordiumNode.GrpcClient;

public class TransactionStatus
{
    public string Sender { get; init; } // Address
    public string Hash { get; init; } // TransactionHash
    public string Cost { get; init; } 
    public int EnergyCost { get; init; }
    public TransactionType Type { get; init; }
    public TransactionResult Result { get; init; }
    public int Index { get; init; }
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