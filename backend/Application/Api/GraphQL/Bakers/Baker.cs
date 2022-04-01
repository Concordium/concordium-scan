using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Bakers;

public class Baker
{
    [ID]
    public long Id { get; set; }
    public long BakerId => Id;
    public BakerStatus Status { get; set; }
    public PendingBakerChange? PendingChange { get; set; }
}
