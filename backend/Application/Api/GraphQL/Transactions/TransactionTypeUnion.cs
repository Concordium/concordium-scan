using Application.Observability;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Transactions;

[UnionType("TransactionType")]
public abstract class TransactionTypeUnion
{
    public static TransactionTypeUnion CreateFrom(IBlockItemSummaryDetails value)
    {
        switch (value)
        {
            case AccountTransactionDetails x:
                var _ = TransactionTypeFactory.TryFrom(x.Effects, out var transactionType);
                return new AccountTransaction { AccountTransactionType = transactionType };
            case AccountCreationDetails x:
                ApplicationMetrics.IncAccountCreated(x.CredentialType);
                return new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = x.CredentialType };
            case UpdateDetails x:
                return new UpdateTransaction { UpdateTransactionType = UpdatePayloadFactory.From(x.Payload) };
            default:
                throw new NotSupportedException($"Cannot map this transaction type");
        }
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

public class AccountTransaction : TransactionTypeUnion
{
    /// <summary>
    /// In some cases when transaction is rejected <see cref="AccountTransactionType"/> can be null and <see cref="TransactionTypeUnion.FromCompactString"/>
    /// maps to '0'. 
    /// </summary>
    public TransactionType? AccountTransactionType { get; init; }
}

public class CredentialDeploymentTransaction : TransactionTypeUnion
{
    /// <summary>
    /// Should always map to non null value.
    ///
    /// It is kept nullable for legacy reasons.
    ///
    /// Those nullable should be cleaned up and it would be those returned from query
    /// <code>
    /// select *
    /// from graphql_transactions
    /// where transaction_type = '1'
    /// </code> 
    /// </summary>
    public CredentialType? CredentialDeploymentTransactionType { get; init; }
}

public class UpdateTransaction : TransactionTypeUnion
{
    /// <summary>
    /// Should always map to non null value.
    ///
    /// It is kept nullable for legacy reasons.
    ///
    /// Those nullable should be cleaned up and it would be those returned from query
    /// <code>
    /// select *
    /// from graphql_transactions
    /// where transaction_type = '2'
    /// </code> 
    /// </summary>
    public UpdateType? UpdateTransactionType { get; init; }
}
