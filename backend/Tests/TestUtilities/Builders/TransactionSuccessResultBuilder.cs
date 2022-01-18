using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class TransactionSuccessResultBuilder
{
    private TransactionResultEvent[] _events = Array.Empty<TransactionResultEvent>();

    public TransactionSuccessResultBuilder WithEvents(params TransactionResultEvent[] value)
    {
        _events = value;
        return this;
    }

    public TransactionSuccessResult Build()
    {
        return new TransactionSuccessResult
        {
            Events = _events
        };
    }
}