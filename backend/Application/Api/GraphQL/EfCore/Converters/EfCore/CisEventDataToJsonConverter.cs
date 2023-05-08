using System.Text.Json;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Tokens;

namespace Application.Api.GraphQL.EfCore.Converters
{
    public class CisEventDataToJsonConverter : ObjectToJsonConverter<CisEventData>
    {
        private static readonly JsonSerializerOptions SerializerOptions;

        static CisEventDataToJsonConverter()
        {
            SerializerOptions = EfCoreJsonSerializerOptionsFactory.Create();
        }

        public CisEventDataToJsonConverter() : base(
            v => Serialize(v, SerializerOptions),
            v => Deserialize(v, SerializerOptions))
        {
        }
    }
}