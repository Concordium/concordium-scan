using System.Collections.Generic;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Import;
using Application.Import.ConcordiumNode.Types.Modules;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;
using SpecialEvent = ConcordiumSdk.NodeApi.Types.SpecialEvent;

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
            _transactionWriter = new TransactionWriter(_dbContextFactory, _metrics, new SmartContractModuleSerDeStub());

            using var connection = dbFixture.GetOpenConnection();
            connection.Execute("TRUNCATE TABLE graphql_accounts");
            connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
            connection.Execute("TRUNCATE TABLE graphql_transactions");
            connection.Execute("TRUNCATE TABLE graphql_transaction_events");
        }

        [Fact]
        public async Task SameBlock_AccountCreation_InwardsTransferTest()
        {
            var senderAccount = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
            ulong senderAccountId = 1;
            var receiverAccount = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
            ulong receiverAccountId = 2;
            var slotTime = DateTimeOffset.UtcNow;
            var block = new Block()
            {
                BlockSlotTime = slotTime,
            };
            var blockDataPayload = new BlockDataPayload(
            new BlockInfo() { BlockSlotTime = block.BlockSlotTime },
            new BlockSummaryV1()
            {
                ProtocolVersion = 4,
                TransactionSummaries = new TransactionSummary[] {
                    new TransactionSummary(
                        senderAccount,
                        new TransactionHash("d71b02cf129cf5f308131823945bdef23474edaea669acb08667e194d4b713ab"),
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
                SpecialEvents = new SpecialEvent[0]
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
            () => Task.FromResult<PoolStatusPassiveDelegation>(null),
            new ModuleSchema[0]
            );

            // Sender Account Should already be present in the database
            await _accountWriter.InsertAccounts(new List<Account> {
                new Account() {
                    Amount = CcdAmount.FromCcd(2).MicroCcdValue,
                    BaseAddress = new Application.Api.GraphQL.Accounts.AccountAddress(senderAccount.GetBaseAddress().AsString),
                    CanonicalAddress = new Application.Api.GraphQL.Accounts.AccountAddress(senderAccount.AsString),
                    CreatedAt = slotTime.AddSeconds(-10),
                    TransactionCount = 0,
                    Id = (long)senderAccountId
                }
            });
            _accountLookup.AddToCache(senderAccount.GetBaseAddress().AsString, (long?)senderAccountId);

            await _accountImportHandler.AddNewAccounts(blockDataPayload.AccountInfos.CreatedAccounts, block.BlockSlotTime);
            var transactions = await _transactionWriter.AddTransactions(blockDataPayload.BlockSummary, block.Id, block.BlockSlotTime);
            _accountImportHandler.HandleAccountUpdates(blockDataPayload, transactions, block);

            var statementEntry = _dbContextFactory.CreateDbContext().AccountStatementEntries.Where(e => e.AccountId == (long)receiverAccountId).Single();
            statementEntry.AccountBalance.Should().Be(CcdAmount.FromCcd(1).MicroCcdValue);
        }
    }
}
