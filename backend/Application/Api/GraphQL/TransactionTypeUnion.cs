using ConcordiumSdk.Types;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

[UnionType("TransactionType")]
public abstract class TransactionTypeUnion
{
    public static TransactionTypeUnion CreateFrom(TransactionType value)
    {
        return value switch
        {
            TransactionType<AccountTransactionType> x => new AccountTransaction { AccountTransactionType = x.Type },
            TransactionType<CredentialDeploymentTransactionType> x => new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = x.Type },
            TransactionType<UpdateTransactionType> x => new UpdateTransaction { UpdateTransactionType = x.Type },
            _ => throw new NotSupportedException($"Cannot map this transaction type")
        };
    }

    public string ToCompactString()
    {
        return this switch
        {
            AccountTransaction x => x.AccountTransactionType.HasValue ? $"0.{(int)x.AccountTransactionType.Value}" : "0",
            CredentialDeploymentTransaction x => x.CredentialDeploymentTransactionType.HasValue ? $"1.{(int)x.CredentialDeploymentTransactionType.Value}" : "1",
            UpdateTransaction x => x.UpdateTransactionType.HasValue ? $"2.{(int)x.UpdateTransactionType.Value}" : "2",
            _ => throw new NotSupportedException($"Transaction type '{GetType().Name}' is not supported.")
        };
    }

    public static TransactionTypeUnion FromCompactString(string value)
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
}

public class AccountTransaction : TransactionTypeUnion
{
    public AccountTransactionType? AccountTransactionType { get; set; }
}

public class CredentialDeploymentTransaction : TransactionTypeUnion
{
    public CredentialDeploymentTransactionType? CredentialDeploymentTransactionType { get; set; }
}

public class UpdateTransaction : TransactionTypeUnion
{
    public UpdateTransactionType? UpdateTransactionType { get; set; }
}
