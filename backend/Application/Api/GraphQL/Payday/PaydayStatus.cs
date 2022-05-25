using HotChocolate;

namespace Application.Api.GraphQL.Payday;

public class PaydayStatus
{
    [GraphQLIgnore]
    public int Id { get; init; }
    
    public DateTimeOffset NextPaydayTime { get; set; }
}