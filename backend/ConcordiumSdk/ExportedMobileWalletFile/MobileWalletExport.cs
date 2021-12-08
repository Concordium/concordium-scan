using System.Linq;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public class MobileWalletExport
{
    [JsonConstructor]
    private MobileWalletExport(Identity[] identities) : this ("", identities)
    {
    }

    public MobileWalletExport(string environment, Identity[] identities)
    {
        Environment = environment;
        Identities = identities ?? throw new ArgumentNullException(nameof(identities));
    }

    public string Environment { get; }
    public Identity[] Identities { get; }
    
    public string GetSingleSignKeyForAccountWithAddress(AccountAddress address)
    {
        return Identities
            .SelectMany(identity => identity.Accounts
                .Where(account => account.Address == address)
                .Select(x => x.AccountKeys.Keys["0"].Keys["0"].SignKey)) // Assume single SignKey is found here
            .Single();
    }

    internal MobileWalletExport WithEnvironment(string environment)
    {
        return new MobileWalletExport(environment, Identities);
    }
}