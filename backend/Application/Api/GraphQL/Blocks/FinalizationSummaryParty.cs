namespace Application.Api.GraphQL.Blocks;

public class FinalizationSummaryParty
{
    public long BakerId { get; init; } 
    public long Weight { get; init; }
    public bool Signed { get; init; }
}