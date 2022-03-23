using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class TransactionTypeToStringConverter : ValueConverter<TransactionTypeUnion, string>
{
    public TransactionTypeToStringConverter() : base(
        v => ConvertToString(v),
        v => ConvertToTransactionTypeUnion(v))
    {
    }

    private static TransactionTypeUnion ConvertToTransactionTypeUnion(string value)
    {
        return TransactionTypeUnion.FromCompactString(value);
    }

    private static string ConvertToString(TransactionTypeUnion value)
    {
        return value.ToCompactString();
    }
}