using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.ChainParametersGraphql
{
    [ExtendObjectType(typeof(Query))]
    public class ChainParametersQuery
    {
        [UseDbContext(typeof(GraphQlDbContext))]
        public async Task<ChainParameters?> GetLatestChainParameters([ScopedService] GraphQlDbContext dbContext)
        {
            return await dbContext.ChainParameters
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }
    }
}