namespace Application.Api.GraphQL;

public class AccountAddress : Address
{
    public AccountAddress(string address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public string Address { get; }
}