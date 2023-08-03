using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Payday;
using Application.Common.Diagnostics;
using Application.Import;
using Concordium.Sdk.Types;
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
        if (payload.BlockInfo.ProtocolVersion.AsInt() < 4) return new NotFirstBlockAfterPayday();
        
        using var counter = _metrics.MeasureDuration(nameof(PaydayImportHandler), nameof(UpdatePaydayStatus));

        if (payload.RewardStatus is not RewardOverviewV1 rewardOverviewV1)
        {
            throw new InvalidOperationException("Expected reward status to be V1");
        }

        // TODO: Optimize performance by keeping the latest read payday status in (transient) import state
        //       Re-attach to context before update!
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var status = await dbContext.PaydayStatuses.SingleOrDefaultAsync();
        if (status == null)
        {
            status = new PaydayStatus
            {
                ProtocolVersion = payload.BlockInfo.ProtocolVersion.AsInt(),
                PaydayStartTime = payload.BlockInfo.BlockSlotTime, // Best guess at a start time for the first payday period!
                NextPaydayTime = rewardOverviewV1.NextPaydayTime
            };
            dbContext.PaydayStatuses.Add(status);
            await dbContext.SaveChangesAsync();
        }
        else if (status.ProtocolVersion != payload.BlockInfo.ProtocolVersion.AsInt())
        {
            status.ProtocolVersion = payload.BlockInfo.ProtocolVersion.AsInt();
            status.PaydayStartTime = payload.BlockInfo.BlockSlotTime;
            status.NextPaydayTime = rewardOverviewV1.NextPaydayTime;
            await dbContext.SaveChangesAsync();
        }
        else if (status.NextPaydayTime != rewardOverviewV1.NextPaydayTime)
        {
            var duration = Convert.ToInt64((status.NextPaydayTime - status.PaydayStartTime).TotalSeconds);
            var result = new FirstBlockAfterPayday(status.NextPaydayTime, duration);
                
            status.PaydayStartTime = status.NextPaydayTime;
            status.NextPaydayTime = rewardOverviewV1.NextPaydayTime;
            status.ProtocolVersion = payload.BlockInfo.ProtocolVersion.AsInt();
            await dbContext.SaveChangesAsync();
            return result;
        }

        return new NotFirstBlockAfterPayday();
    }
    
    public async Task<PaydaySummary?> AddPaydaySummaryOnPayday(BlockImportPaydayStatus importPaydayStatus, Block block)
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

            return result;
        }

        return null;
    }
}

public abstract record BlockImportPaydayStatus;
public record NotFirstBlockAfterPayday : BlockImportPaydayStatus;
public record FirstBlockAfterPayday(DateTimeOffset PaydayTimestamp, long PaydayDurationSeconds) : BlockImportPaydayStatus;
