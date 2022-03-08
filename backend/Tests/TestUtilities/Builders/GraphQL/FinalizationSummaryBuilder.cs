using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class FinalizationSummaryBuilder
{
    private string _finalizedBlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";

    public FinalizationSummary Build()
    {
        return new FinalizationSummary()
        {
            FinalizationDelay = 0,
            FinalizationIndex = 0,
            FinalizedBlockHash = _finalizedBlockHash
        };
    }

    public FinalizationSummaryBuilder WithFinalizedBlockHash(string value)
    {
        _finalizedBlockHash = value;
        return this;
    }
}