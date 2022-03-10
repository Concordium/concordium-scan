using Application.Api.GraphQL;
using Google.Protobuf.WellKnownTypes;

namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountBuilder
{
    private long _id = 0;
    private string _canonicalAddress = "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P";
    private AccountAddress _baseAddress = new("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT00");

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
            CreatedAt = new DateTimeOffset(2021, 10, 10, 12, 0, 0, TimeSpan.Zero)
        };
    }

    public AccountBuilder WithCanonicalAddress(string value)
    {
        _canonicalAddress = value;
        return this;
    }

    public AccountBuilder WithBaseAddress(string addressValue)
    {
        _baseAddress = new AccountAddress(addressValue);
        return this;
    }
}