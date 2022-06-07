using HotChocolate;

namespace Application.Api.GraphQL.Network;

public record PeerReference(
    string NodeId)
{
    [GraphQLDescription("The node status of the peer. Will be null if no status for the peer exists.")]
    public NodeStatus? GetNodeStatus([Service] NodeStatusSnapshot nodeSummarySnapshot)
    {
        var status = nodeSummarySnapshot.NodeStatuses
            .SingleOrDefault(x => x.NodeId == NodeId);
        return status;
    }
}