using ConcordiumSdk.Types;
using HotChocolate;

namespace Application.Api.GraphQL
{
    /// <summary>
    /// Structure used to send messages to the Subscribers <see cref="Subscription.AccountsUpdated(string, AccountsUpdatedSubscriptionItem)"/>
    /// </summary>
    [GraphQLDescription("Structure used to send messages to the Subscribers")]
    public class AccountsUpdatedSubscriptionItem
    {
        /// <summary>
        /// Account Address
        /// </summary>
        [GraphQLDescription("Account Address")]
        public string Address { get; set; }

        public AccountsUpdatedSubscriptionItem(AccountAddress address)
        {
            this.Address = address.AsString;
        }
    }
}