using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL;

public class Account
{
    [ID]
    public long Id { get; set; }
    public string Address { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}