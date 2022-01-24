namespace Application.Api.GraphQL;

public class SearchResult
{
    public Block[] Blocks { get; set; } = Array.Empty<Block>();
    public Transaction[] Transactions { get; set; } = Array.Empty<Transaction>();
}