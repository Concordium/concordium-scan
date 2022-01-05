using ConcordiumSdk.Types;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

[UnionType("TransactionType")]
public abstract class TransactionTypeUnion {}

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
