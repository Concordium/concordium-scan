using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public abstract class TransactionResult
{
    public abstract IEnumerable<AccountAddress> GetAccountAddresses();
}