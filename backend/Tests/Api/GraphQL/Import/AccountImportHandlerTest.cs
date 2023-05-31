using System.Collections.Generic;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Import;
using Concordium.Sdk.Types;
using Concordium.Sdk.Types.New;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using SpecialEvent = Application.Api.GraphQL.Blocks.SpecialEvent;

namespace Tests.Api.GraphQL.Import
{
    public class AccountImportHandlerTest : IClassFixture<DatabaseFixture>
    {
        private readonly NullMetrics _metrics;
        private readonly GraphQlDbContextFactoryStub _dbContextFactory;
        private readonly AccountWriter _accountWriter;
        private readonly AccountLookupStub _accountLookup;
        private readonly AccountImportHandler _accountImportHandler;
        private readonly TransactionWriter _transactionWriter;

        public AccountImportHandlerTest(DatabaseFixture dbFixture)
        {
            _metrics = new NullMetrics();
            _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
            _accountWriter = new AccountWriter(_dbContextFactory, _metrics);
            _accountLookup = new AccountLookupStub();
            _accountImportHandler = new AccountImportHandler(_accountLookup, _metrics, _accountWriter);
            _transactionWriter = new TransactionWriter(_dbContextFactory, _metrics);

            using var connection = dbFixture.GetOpenConnection();
            connection.Execute("TRUNCATE TABLE graphql_accounts");
            connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
            connection.Execute("TRUNCATE TABLE graphql_transactions");
            connection.Execute("TRUNCATE TABLE graphql_transaction_events");
        }

        [Fact]
        public async Task SameBlock_AccountCreation_InwardsTransferTest()
        {
            var senderAccount = Concordium.Sdk.Types.AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
            ulong senderAccountId = 1;
            var receiverAccount = Concordium.Sdk.Types.AccountAddress.From("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
            ulong receiverAccountId = 2;
            var slotTime = DateTimeOffset.UtcNow;
            var block = new Block()
            {
                BlockSlotTime = slotTime,
                // Signifies Non Genesis Block
                BlockHeight = 1
            };
            var blockDataPayload = new BlockDataPayload(
            new BlockInfo() { BlockSlotTime = block.BlockSlotTime },
            new BlockSummaryV1()
            {
                ProtocolVersion = 4,
                TransactionSummaries = new TransactionSummary[] {
                    new TransactionSummary(
                        senderAccount,
                        TransactionHash.From("d71b02cf129cf5f308131823945bdef23474edaea669acb08667e194d4b713ab"),
                        CcdAmount.Zero,
                        0,
                        TransactionType.Get(AccountTransactionType.SimpleTransfer),
                        new TransactionSuccessResult() {
                            Events = new TransactionResultEvent[]
                                {
                                    new Transferred(CcdAmount.FromCcd(1), receiverAccount, senderAccount)
                                }
                        },
                        0),
                },
                SpecialEvents = new Concordium.Sdk.Types.New.SpecialEvent[0]
            },
            // Signifies that the Account Has been created in this block
            new AccountInfosRetrieved(new AccountInfo[] {
                new AccountInfo()
                {
                    AccountNonce = new Nonce(1),
                    AccountAmount = CcdAmount.FromCcd(1),
                    AccountAddress = receiverAccount,
                    AccountIndex = receiverAccountId
                }
            }, new AccountInfo[0]),
            null,
            () => Task.FromResult(new BakerPoolStatus[0]),
            () => Task.FromResult<PoolStatusPassiveDelegation>(null)
            );

            // Sender Account Should already be present in the database
            await _accountWriter.InsertAccounts(new List<Account> {
                new Account() {
                    Amount = CcdAmount.FromCcd(2).Value,
                    BaseAddress = new Application.Api.GraphQL.Accounts.AccountAddress(senderAccount.GetBaseAddress().ToString()),
                    CanonicalAddress = new Application.Api.GraphQL.Accounts.AccountAddress(senderAccount.ToString()),
                    CreatedAt = slotTime.AddSeconds(-10),
                    TransactionCount = 0,
                    Id = (long)senderAccountId
                }
            });
            _accountLookup.AddToCache(senderAccount.GetBaseAddress().ToString(), (long?)senderAccountId);

            await _accountImportHandler.AddNewAccounts(blockDataPayload.AccountInfos.CreatedAccounts, block.BlockSlotTime, block.BlockHeight);
            var transactions = await _transactionWriter.AddTransactions(blockDataPayload.BlockSummary, block.Id, block.BlockSlotTime);
            _accountImportHandler.HandleAccountUpdates(blockDataPayload, transactions, block);

            var statementEntry = _dbContextFactory.CreateDbContext().AccountStatementEntries.Where(e => e.AccountId == (long)receiverAccountId).Single();
            statementEntry.AccountBalance.Should().Be(CcdAmount.FromCcd(1).Value);
        }

        [Fact]
        public async Task GenesisBlock_AccountCreation_BalanceTest()
        {
            var senderAccount = Concordium.Sdk.Types.AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
            ulong senderAccountId = 1;
            var receiverAccount = Concordium.Sdk.Types.AccountAddress.From("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
            ulong receiverAccountId = 2;
            var slotTime = DateTimeOffset.UtcNow;
            var block = new Block()
            {
                BlockSlotTime = slotTime,
                // Signifies Genesis Block
                BlockHeight = 0
            };
            var blockDataPayload = new BlockDataPayload(
            new BlockInfo() { BlockSlotTime = block.BlockSlotTime },
            new BlockSummaryV1()
            {
                ProtocolVersion = 4,
                TransactionSummaries = new TransactionSummary[0],
                SpecialEvents = new Concordium.Sdk.Types.New.SpecialEvent[0]
            },
            // Signifies that the Account Has been created in this block
            new AccountInfosRetrieved(new AccountInfo[] {
                new AccountInfo()
                {
                    AccountNonce = new Nonce(1),
                    AccountAmount = CcdAmount.FromCcd(1),
                    AccountAddress = receiverAccount,
                    AccountIndex = receiverAccountId
                }
            }, new AccountInfo[0]),
            null,
            () => Task.FromResult(new BakerPoolStatus[0]),
            () => Task.FromResult<PoolStatusPassiveDelegation>(null)
            );

            // Sender Account Should already be present in the database
            await _accountWriter.InsertAccounts(new List<Account> {
                new Account() {
                    Amount = CcdAmount.FromCcd(2).Value,
                    BaseAddress = new Application.Api.GraphQL.Accounts.AccountAddress(senderAccount.GetBaseAddress().ToString()),
                    CanonicalAddress = new Application.Api.GraphQL.Accounts.AccountAddress(senderAccount.ToString()),
                    CreatedAt = slotTime.AddSeconds(-10),
                    TransactionCount = 0,
                    Id = (long)senderAccountId
                }
            });
            _accountLookup.AddToCache(senderAccount.GetBaseAddress().ToString(), (long?)senderAccountId);

            await _accountImportHandler.AddNewAccounts(blockDataPayload.AccountInfos.CreatedAccounts, block.BlockSlotTime, block.BlockHeight);
            var transactions = await _transactionWriter.AddTransactions(blockDataPayload.BlockSummary, block.Id, block.BlockSlotTime);
            _accountImportHandler.HandleAccountUpdates(blockDataPayload, transactions, block);

            var statementEntry = _dbContextFactory.CreateDbContext()
                .AccountStatementEntries
                .Where(e => e.AccountId == (long)receiverAccountId)
                .Count()
                .Should()
                .Be(0);
        }
    }
}
