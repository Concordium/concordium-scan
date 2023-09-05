using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Configurations;
using Concordium.Sdk.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Api.GraphQL.Import.Validations;

public class ImportValidationController
{
    private readonly FeatureFlagOptions _featureFlags;
    private readonly IImportValidator[] _validators;

    public ImportValidationController(
        ConcordiumClient grpcNodeClient,
        IDbContextFactory<GraphQlDbContext> dbContextFactory,
        IOptions<FeatureFlagOptions> featureFlagsOptions)
    {
        _featureFlags = featureFlagsOptions.Value;
        _validators = new IImportValidator[]
        {
            new AccountValidator(grpcNodeClient, dbContextFactory),
            new BalanceStatisticsValidator(grpcNodeClient, dbContextFactory),
            new PassiveDelegationValidator(grpcNodeClient, dbContextFactory)
        };
    }

    public async Task PerformValidations(Block block)
    {
        if (!_featureFlags.ConcordiumNodeImportValidationEnabled) return;

        if (block.BlockHeight % 10000 == 0)
        {
            foreach (var validator in _validators)
                await validator.Validate(block);
        }
    }
}