using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountTransactionRelationBuilder
{
    private long _accountId;

    public AccountTransactionRelationBuilder WithAccountId(long value)
    {
        _accountId = value;
        return this;
    }

    public AccountTransactionRelation Build()
    {
        return new AccountTransactionRelation
        {
            AccountId = _accountId
        };
    }
}