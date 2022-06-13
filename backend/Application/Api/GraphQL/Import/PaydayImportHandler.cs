using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Payday;
using Application.Common.Diagnostics;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class PaydayImportHandler
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public PaydayImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<BlockImportPaydayStatus> UpdatePaydayStatus(BlockDataPayload payload)
    {
        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            using var counter = _metrics.MeasureDuration(nameof(PaydayImportHandler), nameof(UpdatePaydayStatus));

            var rewardStatus = payload.RewardStatus as RewardStatusV1;
            if (rewardStatus == null) throw new InvalidOperationException("Expected reward status to be V1");

            // TODO: Optimize performance by keeping the latest read payday status in (transient) import state
            //       Re-attach to context before update!
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var status = await dbContext.PaydayStatuses.SingleOrDefaultAsync();
            if (status == null)
            {
                status = new PaydayStatus
                {
                    PaydayStartTime = payload.BlockInfo.BlockSlotTime, // Best guess at a start time for the first payday period!
                    NextPaydayTime = rewardStatus.NextPaydayTime
                };
                dbContext.PaydayStatuses.Add(status);
                await dbContext.SaveChangesAsync();
            }
            else if (status.NextPaydayTime != rewardStatus.NextPaydayTime)
            {
                var duration = status.NextPaydayTime - status.PaydayStartTime;
                var result = new FirstBlockAfterPayday(status.NextPaydayTime, Convert.ToInt64(duration.TotalSeconds));
                
                status.PaydayStartTime = status.NextPaydayTime;
                status.NextPaydayTime = rewardStatus.NextPaydayTime;
                await dbContext.SaveChangesAsync();
                return result;
            }
        }

        return new NotFirstBlockAfterPayday();
    }
    
    public async Task AddPaydaySummaryOnPayday(BlockImportPaydayStatus importPaydayStatus, Block block)
    {
        if (importPaydayStatus is FirstBlockAfterPayday firstBlockAfterPayday)
        {
            var result = new PaydaySummary
            {
                BlockId = block.Id,
                PaydayTime = firstBlockAfterPayday.PaydayTimestamp,
                PaydayDurationSeconds = firstBlockAfterPayday.PaydayDurationSeconds
            };
            
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.PaydaySummaries.Add(result);
            await dbContext.SaveChangesAsync();
        }
    }
}

public abstract record BlockImportPaydayStatus;
public record NotFirstBlockAfterPayday : BlockImportPaydayStatus;
public record FirstBlockAfterPayday(DateTimeOffset PaydayTimestamp, long PaydayDurationSeconds) : BlockImportPaydayStatus;
