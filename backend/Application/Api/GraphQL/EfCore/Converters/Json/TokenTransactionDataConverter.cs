using Application.Api.GraphQL.Tokens;
using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class TokenTransactionDataConverter : PolymorphicJsonConverter<CisEventData>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(CisEventDataBurn), 1 },
        { typeof(CisEventDataMetadataUpdate), 2 },
        { typeof(CisEventDataMint), 3 },
        { typeof(CisEventDataTransfer), 4 },
    };

    public TokenTransactionDataConverter() : base(SerializeMap)
    {
    }
}