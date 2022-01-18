using ConcordiumSdk.Types;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters;

public class TransactionTypeToStringConverter : ValueConverter<TransactionTypeUnion, string>
{
    public TransactionTypeToStringConverter() : base(
        v => ConvertToString(v),
        v => ConvertToTransactionTypeUnion(v))
    {
    }

    private static TransactionTypeUnion ConvertToTransactionTypeUnion(string value)
    {
        var split = value.Split(".");
        return split[0] switch
        {
            "0" => split.Length == 2 ? new AccountTransaction { AccountTransactionType = (AccountTransactionType)int.Parse(split[1])} : new AccountTransaction(),
            "1" => split.Length == 2 ? new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = (CredentialDeploymentTransactionType)int.Parse(split[1])} : new CredentialDeploymentTransaction(),
            "2" => split.Length == 2 ? new UpdateTransaction { UpdateTransactionType = (UpdateTransactionType)int.Parse(split[1])} : new UpdateTransaction(),
            _ => throw new NotSupportedException($"Transaction type value '{value}' is not supported.")
        };
    }

    private static string ConvertToString(TransactionTypeUnion value)
    {
        return value switch
        {
            AccountTransaction x => x.AccountTransactionType.HasValue ? $"0.{(int)x.AccountTransactionType.Value}" : "0",
            CredentialDeploymentTransaction x => x.CredentialDeploymentTransactionType.HasValue ? $"1.{(int)x.CredentialDeploymentTransactionType.Value}" : "1",
            UpdateTransaction x => x.UpdateTransactionType.HasValue ? $"2.{(int)x.UpdateTransactionType.Value}" : "2",
            _ => throw new NotSupportedException($"Transaction type '{value.GetType().Name}' is not supported.")
        };
    }
}