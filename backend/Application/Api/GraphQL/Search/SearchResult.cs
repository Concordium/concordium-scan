using HotChocolate.Types;

namespace Application.Api.GraphQL.Search;

public class SearchResult
{
    [UsePaging]
    public Block[] Blocks { get; set; } = Array.Empty<Block>();
    [UsePaging]
    public Transaction[] Transactions { get; set; } = Array.Empty<Transaction>();
    [UsePaging]
    public Account[] Accounts { get; set; } = Array.Empty<Account>();
}