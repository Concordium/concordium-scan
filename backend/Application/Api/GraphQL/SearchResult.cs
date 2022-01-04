namespace Application.Api.GraphQL;

public class SearchResult
{
    public IEnumerable<Block> Blocks { get; set; } = Array.Empty<Block>();
    public IEnumerable<Transaction> Transactions { get; set; } = Array.Empty<Transaction>();
}