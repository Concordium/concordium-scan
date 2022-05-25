using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Payday;
using Application.Common.Diagnostics;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;
using PaydayAccountRewardSpecialEvent = ConcordiumSdk.NodeApi.Types.PaydayAccountRewardSpecialEvent;
using PaydayFoundationRewardSpecialEvent = ConcordiumSdk.NodeApi.Types.PaydayFoundationRewardSpecialEvent;

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

    public async Task UpdatePaydayStatus(BlockDataPayload payload, Block block)
    {
        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            using var counter = _metrics.MeasureDuration(nameof(PaydayImportHandler), nameof(UpdatePaydayStatus));

            await UpdateRewardStatus(payload);
            await AddPaydaySummaryOnPayday(payload, block);
        }
    }

    private async Task UpdateRewardStatus(BlockDataPayload payload)
    {
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

    private async Task AddPaydaySummaryOnPayday(BlockDataPayload payload, Block block)
    {
        var paydayEvents = payload.BlockSummary.SpecialEvents
            .Where(x => x is PaydayAccountRewardSpecialEvent or PaydayFoundationRewardSpecialEvent)
            .ToArray();

        if (paydayEvents.Length > 0)
        {
            ulong totalTransactionFees = 0;
            ulong totalBakerRewards = 0;
            ulong totalFinalizationRewards = 0;
            ulong totalDevelopmentCharge = 0;
            
            foreach (var paydayEvent in paydayEvents)
            {
                if (paydayEvent is PaydayFoundationRewardSpecialEvent foundationRewards)
                {
                    totalDevelopmentCharge += foundationRewards.DevelopmentCharge.MicroCcdValue;
                }
                else if (paydayEvent is PaydayAccountRewardSpecialEvent accountRewards)
                {
                    totalTransactionFees += accountRewards.TransactionFees.MicroCcdValue;
                    totalBakerRewards += accountRewards.BakerReward.MicroCcdValue;
                    totalFinalizationRewards += accountRewards.FinalizationReward.MicroCcdValue;
                }
                else
                    throw new NotImplementedException();
            }

            var result = new PaydaySummary
            {
                BlockId = block.Id
            };
            
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.PaydaySummaries.Add(result);
            await dbContext.SaveChangesAsync();
        }
    }
}