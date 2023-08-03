
using Concordium.Sdk.Types;

namespace Application.Common;

public class NonCirculatingAccounts
{
    public IEnumerable<AccountAddress> accounts { get; init; }

    public NonCirculatingAccounts(IEnumerable<AccountAddress> accounts)
    {
        this.accounts = accounts;
    }
}
