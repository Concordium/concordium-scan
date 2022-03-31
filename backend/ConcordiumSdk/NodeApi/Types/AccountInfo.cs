using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class AccountInfo
{
    public Nonce AccountNonce { get; init; }
    public CcdAmount AccountAmount { get; init; }
    public ulong AccountIndex { get; init; }
    public AccountAddress AccountAddress { get; init; }
    public AccountBaker? AccountBaker { get; init; }
}