using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerBuilder
{
    private long _id = 10;
    private PendingBakerChange? _pendingBakerChange = null;
    private BakerStatus _bakerStatus = BakerStatus.Active;

    public Baker Build()
    {
        return new Baker
        {
            Id = _id,
            PendingBakerChange = _pendingBakerChange,
            Status = _bakerStatus
        };
    }

    public BakerBuilder WithId(long value)
    {
        _id = value;
        return this;
    }

    public BakerBuilder WithPendingBakerChange(PendingBakerChange? value)
    {
        _pendingBakerChange = value;
        return this;
    }

    public BakerBuilder WithStatus(BakerStatus value)
    {
        _bakerStatus = value;
        return this;
    }
}