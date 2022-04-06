using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BalanceStatisticsBuilder
{
    private ulong _totalAmount = 0;
    private ulong _totalAmountEncrypted = 0;
    private ulong _totalAmountStaked = 0;

    public BalanceStatistics Build()
    {
        return new BalanceStatistics(_totalAmount, 0, _totalAmountEncrypted, 0, _totalAmountStaked, 0, 0, 0);
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
}