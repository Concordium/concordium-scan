using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

public class Baker
{
    [ID]
    public long Id { get; set; }
    public long BakerId => Id;
    public BakerStatus Status { get; set; }
    public PendingBakerChange? PendingChange { get; set; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Task<Account> GetAccount([ScopedService] GraphQlDbContext dbContext)
    {
        // Account and baker share the same ID!
        return dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(x => x.Id == Id);
    }
}
