using Application.Api.GraphQL.Blocks;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BalanceStatisticsBuilder
{
    private ulong _totalAmount = 0;
    private ulong _totalAmountEncrypted = 0;
    private ulong _totalAmountStaked = 0;
    private ulong _totalAmountStakedByBakers = 0;
    private ulong _totalAmountStakedByDelegation = 0;
    private ulong? _totalAmountReleased = 0;

    public BalanceStatistics Build()
    {
        return new BalanceStatistics(
            _totalAmount, 
            _totalAmountReleased,
            0,
            _totalAmountEncrypted, 
            0, 
            _totalAmountStaked, 
            _totalAmountStakedByBakers, 
            _totalAmountStakedByDelegation, 
            0, 
            0, 
            0);
    }

    public BalanceStatisticsBuilder WithTotalAmount(ulong value)
    {
        _totalAmount = value;
        return this;
    }

    public BalanceStatisticsBuilder WithTotalAmountEncrypted(ulong value)
    {
        _totalAmountEncrypted = value;
        return this;
    }

    public BalanceStatisticsBuilder WithTotalAmountStaked(ulong value)
    {
        _totalAmountStaked = value;
        return this;
    }

    public BalanceStatisticsBuilder WithTotalAmountReleased(ulong? value)
    {
        _totalAmountReleased = value;
        return this;
    }
}
