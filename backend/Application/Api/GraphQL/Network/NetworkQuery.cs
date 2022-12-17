using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Network;

[ExtendObjectType(typeof(Query))]
public class NetworkQuery
{
    private const int MaxSemverLength = 1024;

    [UsePaging]
    public IEnumerable<NodeStatus> GetNodeStatuses(
        [Service] NodeStatusSnapshot nodeSummarySnapshot,
        NodeSortField sortField,
        NodeSortDirection sortDirection)
    {
        var statuses = nodeSummarySnapshot.NodeStatuses.AsQueryable();
        statuses = sortField switch
        {
            NodeSortField.AveragePing => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.AveragePing) : statuses.OrderByDescending(s => s.AveragePing),
            NodeSortField.BlocksReceivedCount => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.BlocksReceivedCount) : statuses.OrderByDescending(s => s.BlocksReceivedCount),
            NodeSortField.ClientVersion => sortDirection == NodeSortDirection.ASC
                ? statuses.OrderBy(s => Semver.SemVersion.Parse(s.ClientVersion, Semver.SemVersionStyles.Any, MaxSemverLength), Semver.SemVersion.SortOrderComparer)
                : statuses.OrderByDescending(s => Semver.SemVersion.Parse(s.ClientVersion, Semver.SemVersionStyles.Any, MaxSemverLength), Semver.SemVersion.SortOrderComparer),
            NodeSortField.ConsensusBakerId => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.ConsensusBakerId) : statuses.OrderByDescending(s => s.ConsensusBakerId),
            NodeSortField.FinalizedBlockHeight => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.FinalizedBlockHeight) : statuses.OrderByDescending(s => s.FinalizedBlockHeight),
            NodeSortField.NodeName => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.NodeName) : statuses.OrderByDescending(s => s.NodeName),
            NodeSortField.PeersCount => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.PeersCount) : statuses.OrderByDescending(s => s.PeersCount),
            NodeSortField.Uptime => sortDirection == NodeSortDirection.ASC ? statuses.OrderBy(s => s.Uptime) : statuses.OrderByDescending(s => s.Uptime),
            _ => throw new NotImplementedException()
        };

        return statuses.AsEnumerable();
    }

    public NodeStatus? GetNodeStatus([Service] NodeStatusSnapshot nodeSummarySnapshot, [ID] string id)
    {
        return nodeSummarySnapshot.NodeStatuses
            .SingleOrDefault(x => x.Id == id);
    }
}