using Application.Api.GraphQL.Network;

namespace Application.Api.GraphQL.ImportNodeStatus;

public class NodeStatusRepository
{
    public NodeStatus[] AllNodeStatuses { get; set; } = Array.Empty<NodeStatus>();
}