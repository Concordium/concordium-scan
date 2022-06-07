using Application.Api.GraphQL.ImportNodeStatus;

namespace Application.Api.GraphQL.Network;

/// <summary>
/// The purpose of the snap-shot is to ensure that all queries within the same request operate on the same
/// snap-shot of the node status.
/// </summary>
public class NodeStatusSnapshot
{
    private readonly Lazy<NodeStatus[]> _data;

    public NodeStatusSnapshot(NodeStatusRepository repository)
    {
        _data = new Lazy<NodeStatus[]>(() => repository.AllNodeStatuses, true);
    }

    public NodeStatus[] NodeStatuses => _data.Value;
}