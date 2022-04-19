using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Rest;

[ApiController]
public class BalanceStatisticsController : ControllerBase
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public BalanceStatisticsController(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("rest/balance-statistics/latest")]
    public async Task<dynamic> GetLatest()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var block = await dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        return new BalanceStatisticsResponse
        (
            block.BlockHeight,
            block.BlockHash,
            block.BlockSlotTime,
            block.BalanceStatistics.TotalAmount,
            block.BalanceStatistics.TotalAmountReleased,
            block.BalanceStatistics.TotalAmountStaked,
            block.BalanceStatistics.TotalAmountEncrypted,
            block.BalanceStatistics.TotalAmountLockedInReleaseSchedules
        );
    }

    private record BalanceStatisticsResponse(
            long BlockHeight, 
            string BlockHash, 
            DateTimeOffset BlockSlotTime,
            ulong TotalAmount, 
            ulong? TotalAmountReleased,
            ulong TotalAmountStaked, 
            ulong TotalAmountEncrypted,
            ulong TotalAmountLockedInReleaseSchedules);
}