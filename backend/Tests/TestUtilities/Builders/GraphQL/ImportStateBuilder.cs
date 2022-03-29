using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class ImportStateBuilder
{
    public ImportState Build()
    {
        return new ImportState()
        {
            GenesisBlockHash = "9dd9ca4d19e9393877d2c44b70f89acbfc0883c2243e5eeaecc0d1cd0503f478"
        };
    }
}