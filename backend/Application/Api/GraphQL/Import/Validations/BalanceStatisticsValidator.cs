using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class BalanceStatisticsValidator : IImportValidator
{
    private readonly GrpcNodeClient _grpcNodeClient;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public BalanceStatisticsValidator(GrpcNodeClient grpcNodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _grpcNodeClient = grpcNodeClient;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<BalanceStatisticsValidator>();
    }

    public async Task Validate(Block block)
    {
        var nodeData = await _grpcNodeClient.GetRewardStatusAsync(new BlockHash(block.BlockHash));
        if (nodeData is RewardStatusV1 rv1)
        {
            var mappedNode = new
            {
                TotalStaked = rv1.TotalStakedCapital.MicroCcdValue
            };
        
            var mappedDb = new
            {
                TotalStaked = block.BalanceStatistics.TotalAmountStaked
            };

            var equals = mappedNode.Equals(mappedDb);
            _logger.Information($"Total staked equals: {equals}");
            if (!equals)
            {
                _logger.Information($"Node data: {mappedNode}");
                _logger.Information($"Database data: {mappedDb}");
            }
        }
    }
}