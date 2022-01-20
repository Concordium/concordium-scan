using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

public static class SchemaConfiguration
{
    public static void Configure(ISchemaBuilder builder)
    {
        builder.AddQueryType<Query>();
        
        builder.BindClrType<ulong, UnsignedLongType>();

        // Bind all concrete types of GraphQL unions and interfaces
        AddAllTypesDerivedFrom<TransactionResult>(builder);
        AddAllTypesDerivedFrom<TransactionTypeUnion>(builder);
        AddAllTypesDerivedFrom<Address>(builder);
        AddAllTypesDerivedFrom<TransactionResultEvent>(builder);
        AddAllTypesDerivedFrom<TransactionRejectReason>(builder);
    }

    private static void AddAllTypesDerivedFrom<T>(ISchemaBuilder builder)
    {
        var result = typeof(T).Assembly.GetExportedTypes()
            .Where(type => type.IsAssignableTo(typeof(T)) && type != typeof(T))
            .ToArray();
        
        builder.AddTypes(result);
    }
}
