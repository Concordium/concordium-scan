namespace Application.Api.GraphQL;

public class FinalizationSummaryParty
{
    public long BakerId { get; init; } 
    public long Weight { get; init; }
    public bool Signed { get; init; }
}