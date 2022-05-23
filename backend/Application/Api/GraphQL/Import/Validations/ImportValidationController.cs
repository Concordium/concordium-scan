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
            new BalanceStatisticsValidator(grpcNodeClient, dbContextFactory),
            new PassiveDelegationValidator(dbContextFactory)
        };
    }

    public async Task PerformValidations(Block block)
    {
        if (!_settings.Enabled) return;

        // TODO: temporarily increase occurrence of validation in blocks after P4 update (testnet)
        var modValue = block.BlockHeight < 3221721 ? 10000 : 1000;
        if (block.BlockHeight % modValue == 0)
        {
            foreach (var validator in _validators)
                await validator.Validate(block);
        }
    }
}