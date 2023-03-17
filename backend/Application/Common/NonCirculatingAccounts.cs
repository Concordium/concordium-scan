
namespace Application.Common;

using ConcordiumSdk.Types;

public class NonCirculatingAccounts
{
    public IEnumerable<AccountAddress> accounts { get; init; }

    public NonCirculatingAccounts(IEnumerable<AccountAddress> accounts)
    {
        this.accounts = accounts;
    }
}
