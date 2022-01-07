using System.Text.Json;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class TransactionSuccessResult : TransactionResult
{
    public TransactionResultEvent[] Events { get; init; }
}

public abstract class TransactionResultEvent
{
}

public class JsonTransactionResultEvent : TransactionResultEvent
{
    public JsonTransactionResultEvent(JsonElement data)
    {
        Data = data;
    }

    public JsonElement Data { get; }
}

public class Transferred : TransactionResultEvent
{
    public Transferred(Address to, Address @from, CcdAmount amount)
    {
        To = to;
        From = @from;
        Amount = amount;
    }

    public CcdAmount Amount { get; }
    public Address To { get; }
    public Address From { get; }
}

