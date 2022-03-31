namespace Application.Api.GraphQL;

public class Baker
{
    public long Id { get; set; }
    public BakerStatus Status { get; set; }
    public PendingBakerChange? PendingBakerChange { get; set; }
}

public abstract record PendingBakerChange(DateTimeOffset EffectiveTime);

public record PendingBakerRemoval(DateTimeOffset EffectiveTime) : PendingBakerChange(EffectiveTime);

public enum BakerStatus
{
    Active = 1,
    Removed = 2
}