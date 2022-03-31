using System.Text.Json;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class PendingBakerChangeToJsonConverter : ObjectToJsonConverter<PendingBakerChange>
{
    private static readonly JsonSerializerOptions SerializerOptions;

    static PendingBakerChangeToJsonConverter()
    {
        SerializerOptions = EfCoreJsonSerializerOptionsFactory.Create();
    }
    
    public PendingBakerChangeToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}