using ConcordiumSdk.Types;

namespace Application.Api.GraphQL
{
    /// <summary>
    /// Structure used to send messages to the Subscribers <see cref="Subscription.AccountsUpdated(string, AccountsUpdatedSubscriptionItem)"/>
    /// </summary>
    public class AccountsUpdatedSubscriptionItem
    {
        /// <summary>
        /// Account Address
        /// </summary>
        public string Address { get; set; }

        public AccountsUpdatedSubscriptionItem(AccountAddress address)
        {
            this.Address = address.AsString;
        }
    }
}