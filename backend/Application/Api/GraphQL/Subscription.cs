using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class Subscription
{
    [Subscribe]
    public Block BlockAdded([EventMessage] Block block) => block;
}