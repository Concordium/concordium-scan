namespace ConcordiumSdk.NodeApi.Types;

public record UpdateQueue<T>(
    ulong NextSequenceNumber,
    ScheduledUpdate<T>[] Queue);