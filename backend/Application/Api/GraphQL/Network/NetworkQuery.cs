using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Network;

[ExtendObjectType(typeof(Query))]
public class NetworkQuery
{
    [UsePaging]
    public IEnumerable<NodeStatus> GetNodeStatuses([Service] NodeStatusSnapshot nodeSummarySnapshot)
    {
        return nodeSummarySnapshot.NodeStatuses;
    }
    
    public NodeStatus? GetNodeStatus([Service] NodeStatusSnapshot nodeSummarySnapshot, [ID] string id)
    {
        return nodeSummarySnapshot.NodeStatuses
            .SingleOrDefault(x => x.Id == id);
    }
}