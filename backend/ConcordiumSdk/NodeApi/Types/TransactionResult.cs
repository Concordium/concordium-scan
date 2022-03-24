using System.Linq;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public abstract class TransactionResult
{
    public abstract IEnumerable<AccountAddress> GetAccountAddresses();

    public virtual IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        return Enumerable.Empty<AccountBalanceUpdate>();
    }
}