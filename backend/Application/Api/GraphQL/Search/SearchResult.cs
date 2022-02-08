namespace Application.Api.GraphQL.Search;

public class SearchResult
{
    public Block[] Blocks { get; set; } = Array.Empty<Block>();
    public Transaction[] Transactions { get; set; } = Array.Empty<Transaction>();
    public Account[] Accounts { get; set; } = Array.Empty<Account>();
}