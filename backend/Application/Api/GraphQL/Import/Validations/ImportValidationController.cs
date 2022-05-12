using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class ImportValidationController
{
    private readonly ImportValidationSettings _settings;
    private readonly IImportValidator[] _validators;

    public ImportValidationController(GrpcNodeClient grpcNodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory, ImportValidationSettings settings)
    {
        _settings = settings;
        _validators = new IImportValidator[]
        {
            new AccountValidator(grpcNodeClient, dbContextFactory),
            new BalanceStatisticsValidator(grpcNodeClient, dbContextFactory)
        };
    }

    public async Task PerformValidations(Block block)
    {
        if (!_settings.Enabled) return;

        if (block.BlockHeight % 10000 == 0)
        {
            foreach (var validator in _validators)
                await validator.Validate(block);
        }
    }
}