using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class ImportStateBuilder
{
    private int _totalBakerCount = 42;
    private DateTime _lastBlockSlotTime = DateTime.Now;
    private long _maxImportedBlockHeight = 0;

    public ImportState Build()
    {
        return new ImportState()
        {
            GenesisBlockHash = "9dd9ca4d19e9393877d2c44b70f89acbfc0883c2243e5eeaecc0d1cd0503f478",
            TotalBakerCount = _totalBakerCount,
            LastBlockSlotTime = _lastBlockSlotTime,
            MaxImportedBlockHeight = _maxImportedBlockHeight
        };
    }

    public ImportStateBuilder WithMaxImportedBlockHeight(long height)
    {
        _maxImportedBlockHeight = height;
        return this;
    }

    public ImportStateBuilder WithLastBlockSlotTime(DateTime time)
    {
        _lastBlockSlotTime = time;
        return this;
    }

    public ImportStateBuilder WithTotalBakerCount(int value)
    {
        _totalBakerCount = value;
        return this;
    }
}