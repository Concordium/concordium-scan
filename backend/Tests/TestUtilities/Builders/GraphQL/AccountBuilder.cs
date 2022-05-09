using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;


namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountBuilder
{
    private long _id = 0;
    private AccountAddress _canonicalAddress = new ("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    private AccountAddress _baseAddress = new("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT00");
    private ulong _amount = 0;
    private int _transactionCount = 0;
    private Delegation? _delegation = null;

    public AccountBuilder WithId(long value)
    {
        _id = value;
        return this;
    }

    public Account Build()
    {
        return new Account
        {
            Id = _id,
            BaseAddress = _baseAddress,
            CanonicalAddress = _canonicalAddress,
            CreatedAt = new DateTimeOffset(2021, 10, 10, 12, 0, 0, TimeSpan.Zero),
            Amount = _amount,
            TransactionCount = _transactionCount,
            Delegation = _delegation
        };
    }

    public AccountBuilder WithCanonicalAddress(string value, bool updateBaseAddress = false)
    {
        _canonicalAddress = new AccountAddress(value);
        if (updateBaseAddress)
            _baseAddress = new AccountAddress(AccountAddressHelper.GetBaseAddress(value));
        return this;
    }

    public AccountBuilder WithBaseAddress(string addressValue)
    {
        _baseAddress = new AccountAddress(addressValue);
        return this;
    }

    public AccountBuilder WithAmount(ulong value)
    {
        _amount = value;
        return this;
    }

    public AccountBuilder WithTransactionCount(int value)
    {
        _transactionCount = value;
        return this;
    }

    public AccountBuilder WithUniqueAddress()
    {
        _canonicalAddress = new (AccountAddressHelper.GetUniqueAddress());
        _baseAddress = new (AccountAddressHelper.GetBaseAddress(_canonicalAddress.AsString));
        return this;
    }

    public AccountBuilder WithDelegation(Delegation? value)
    {
        _delegation = value;
        return this;
    }
}