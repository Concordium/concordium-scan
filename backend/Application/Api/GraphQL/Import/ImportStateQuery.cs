using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Import
{
    [ExtendObjectType(typeof(Query))]
    public class ImportStateQuery
    {
        [UseDbContext(typeof(GraphQlDbContext))]
        public ImportState? GetImportState([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.ImportState.SingleOrDefault();
        }
    }
}
