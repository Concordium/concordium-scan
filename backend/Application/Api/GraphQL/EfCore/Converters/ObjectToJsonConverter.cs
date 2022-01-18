using System.Buffers;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters;

public abstract class ObjectToJsonConverter<T> : ValueConverter<T, string>
{
    protected ObjectToJsonConverter(Expression<Func<T, string>> convertToProviderExpression, Expression<Func<string, T>> convertFromProviderExpression, ConverterMappingHints? mappingHints = null) : base(convertToProviderExpression, convertFromProviderExpression, mappingHints)
    {
    }

    protected static T Deserialize(string json, JsonSerializerOptions serializerOptions)
    {
        return JsonSerializer.Deserialize<T>(json, serializerOptions)!;
    }

    protected static string Serialize(T value, JsonSerializerOptions serializerOptions)
    {
        return JsonSerializer.Serialize(value, serializerOptions);
    }
}