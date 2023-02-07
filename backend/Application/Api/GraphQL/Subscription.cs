using Application.Api.GraphQL.Blocks;
using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class Subscription
{
    [Subscribe]
    public AccountsUpdatedSubscriptionItem AccountsUpdated(
        [Topic] string accountAddress,
        [EventMessage] AccountsUpdatedSubscriptionItem message) => message;

    [Subscribe]
    public Block BlockAdded([EventMessage] Block block) => block;
}