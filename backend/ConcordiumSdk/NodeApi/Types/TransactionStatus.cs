namespace ConcordiumSdk.NodeApi.Types;

public class TransactionStatus
{
    public TransactionStatusType Status { get; init; }
}

public enum TransactionStatusType
{
    Received,
    Committed,
    Finalized
}