using Application.NodeApi;

namespace Tests.TestUtilities.Builders;

public class TransactionRejectResultBuilder
{
    private TransactionRejectReason _rejectReason = new ModuleNotWf();

    public TransactionRejectResultBuilder WithRejectReason(TransactionRejectReason value)
    {
        _rejectReason = value;
        return this;
    }

    public TransactionRejectResult Build()
    {
        return new TransactionRejectResult
        {
            Reason = _rejectReason
        };
    }
}