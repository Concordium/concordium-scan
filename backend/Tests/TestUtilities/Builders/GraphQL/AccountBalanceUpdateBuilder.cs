using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountBalanceUpdateBuilder
{
    private AccountAddress _accountAddress = new("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
    private long _amountAdjustment = 0;

    public AccountBalanceUpdateBuilder WithAccountAddress(AccountAddress value)
    {
        _accountAddress = value;
        return this;
    }

    public AccountBalanceUpdateBuilder WithAmountAdjustment(long value)
    {
        _amountAdjustment = value;
        return this;
    }

    public AccountBalanceUpdate Build()
    {
        return new AccountBalanceUpdate(_accountAddress, _amountAdjustment, BalanceUpdateType.TransferIn);
    }
}