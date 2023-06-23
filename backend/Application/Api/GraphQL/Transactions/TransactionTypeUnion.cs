using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Transactions;

[UnionType("TransactionType")]
public abstract class TransactionTypeUnion
{
    public static TransactionTypeUnion CreateFrom(IBlockItemSummaryDetails value)
    {
        return value switch
        {
            AccountTransactionDetails x => new AccountTransaction { AccountTransactionType = TransactionTypeFactory.From(x.Effects) },
            AccountCreationDetails x => new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = x.CredentialType },
            UpdateDetails x => new UpdateTransaction { UpdateTransactionType = UpdatePayloadFactory.From(x.Payload) },
            _ => throw new NotSupportedException($"Cannot map this transaction type")
        };
    }

    [GraphQLIgnore] // Not part of GraphQL schema!
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
            "0" => split.Length == 2 ? new AccountTransaction { AccountTransactionType = (TransactionType)int.Parse(split[1])} : new AccountTransaction(),
            "1" => split.Length == 2 ? new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = (CredentialType)int.Parse(split[1])} : new CredentialDeploymentTransaction(),
            "2" => split.Length == 2 ? new UpdateTransaction { UpdateTransactionType = (UpdateType)int.Parse(split[1])} : new UpdateTransaction(),
            _ => throw new NotSupportedException($"Transaction type value '{value}' is not supported.")
        };
    }
}

// TODO: Follow up why these are nullable
public class AccountTransaction : TransactionTypeUnion
{
    public TransactionType? AccountTransactionType { get; init; }
}

public class CredentialDeploymentTransaction : TransactionTypeUnion
{
    public CredentialType? CredentialDeploymentTransactionType { get; init; }
}

public class UpdateTransaction : TransactionTypeUnion
{
    public UpdateType? UpdateTransactionType { get; init; }
}
