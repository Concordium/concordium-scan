using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountRewardSummaryBuilder
{
    private long _accountId = 10;
    private long _totalAmount = 0;
    private RewardTypeAmount[] _totalAmountByType = Array.Empty<RewardTypeAmount>();

    public AccountRewardSummary Build()
    {
        return new AccountRewardSummary(_accountId, _totalAmount, _totalAmountByType);
    }

    public AccountRewardSummaryBuilder WithAccountId(long value)
    {
        _accountId = value;
        return this;
    }

    public AccountRewardSummaryBuilder WithTotalAmount(long value)
    {
        _totalAmount = value;
        return this;
    }
    
    public AccountRewardSummaryBuilder WithTotalAmountByType(params RewardTypeAmount[] value)
    {
        _totalAmountByType = value;
        return this;
    }
}