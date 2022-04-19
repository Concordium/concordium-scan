using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class ImportValidationController
{
    private readonly ImportValidationSettings _settings;
    private readonly AccountValidator _accountValidator;

    public ImportValidationController(GrpcNodeClient grpcNodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory, ImportValidationSettings settings)
    {
        _settings = settings;
        _accountValidator = new AccountValidator(grpcNodeClient, dbContextFactory);
    }

    public async Task PerformValidations(Block block)
    {
        if (!_settings.Enabled) return;
        
        if (block.BlockHeight % 10000 == 0)
            await _accountValidator.ValidateAccounts((ulong)block.BlockHeight);
        await _accountValidator.ValidateBaker2((ulong)block.BlockHeight);
    }
}