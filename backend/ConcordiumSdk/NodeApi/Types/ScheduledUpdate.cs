using ConcordiumSdk.NodeApi.Types.JsonConverters;

namespace ConcordiumSdk.NodeApi.Types;

public record ScheduledUpdate<T>(
    UnixTimeSeconds EffectiveTime,  
    T Update);