using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Modules;
using Application.Import.ConcordiumNode.Types.Modules;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.EventLogs
{
    public class ModuleSchemaWriter
    {
        private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
        private readonly ILogger _logger;


        public ModuleSchemaWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _logger = Log.ForContext(GetType());
        }

        public async Task<IEnumerable<ContractModuleSchema>> AddModuleSchemas(IEnumerable<ModuleSchema> moduleSchemas)
        {
            if (!moduleSchemas.Any())
            {
                return new List<ContractModuleSchema>();
            }

            using var db = await _dbContextFactory.CreateDbContextAsync();
            var contractModuleSchemas = moduleSchemas.Select(s => new ContractModuleSchema(s.ModuleRef, s.SchemaName, s.SchemaHex));
            db.SmartContractModuleSchemas.AddRange(contractModuleSchemas);
            await db.SaveChangesAsync();

            return contractModuleSchemas;
        }
    }
}
