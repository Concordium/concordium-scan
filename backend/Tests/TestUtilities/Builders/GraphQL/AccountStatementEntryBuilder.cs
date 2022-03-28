using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class AccountStatementEntryBuilder
{
    private long _accountId = 1;
    private long _amount = 1024;
    private DateTimeOffset _timestamp = new(2022, 01, 01, 12, 0, 0, TimeSpan.Zero);
    private long _blockId = 21;
    private long? _transactionId = 123;
    private AccountStatementEntryType _entryType = AccountStatementEntryType.TransferOut;

    public AccountStatementEntry Build()
    {
        return new AccountStatementEntry
        {
            AccountId = _accountId,
            Amount = _amount,
            Timestamp = _timestamp,
            BlockId = _blockId,
            TransactionId = _transactionId,
            EntryType = _entryType

        };
    }

    public AccountStatementEntryBuilder WithAccountId(long value)
    {
        _accountId = value;
        return this;
    }

    public AccountStatementEntryBuilder WithTransactionId(long value)
    {
        _transactionId = value;
        return this;
    }

    public AccountStatementEntryBuilder WithAmount(long value)
    {
        _amount = value;
        return this;
    }
}