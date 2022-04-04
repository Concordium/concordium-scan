namespace ConcordiumSdk.NodeApi.Types;

public class AccountBaker
{
    public ulong BakerId { get; init; }
    public AccountBakerPendingChange PendingChange { get; init; }
    public bool RestakeEarnings { get; init; }
}