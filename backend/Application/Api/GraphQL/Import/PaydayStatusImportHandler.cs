using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Payday;
using Application.Common.Diagnostics;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class PaydayStatusImportHandler
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public PaydayStatusImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task UpdatePaydayStatus(BlockDataPayload payload)
    {
        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            using var counter = _metrics.MeasureDuration(nameof(PaydayStatusImportHandler), nameof(UpdatePaydayStatus));

            var rewardStatus = payload.RewardStatus as RewardStatusV1;
            if (rewardStatus == null) throw new InvalidOperationException("Expected reward status to be V1");

            // TODO: Optimize performance by keeping the latest read payday status in (transient) import state
            //       Re-attach to context before update!
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var status = await dbContext.PaydayStatuses.SingleOrDefaultAsync();
            if (status == null)
            {
                status = new PaydayStatus();
                dbContext.PaydayStatuses.Add(status);
            }

            status.NextPaydayTime = rewardStatus.NextPaydayTime;
            await dbContext.SaveChangesAsync();
        }
    }
}