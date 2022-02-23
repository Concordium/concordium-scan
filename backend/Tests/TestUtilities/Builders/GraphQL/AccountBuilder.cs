using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountBuilder
{
    private long _id = 1;
    private string _address = "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P";

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
            Address = _address,
            CreatedAt = new DateTimeOffset(2021, 10, 10, 12, 0, 0, TimeSpan.Zero)
        };
    }

    public AccountBuilder WithAddress(string value)
    {
        _address = value;
        return this;
    }
}