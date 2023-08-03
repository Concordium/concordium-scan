using System.Collections.Generic;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Import.EventLogs;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;


namespace Tests.Api.GraphQL.Import.EventLog
{
    [Collection(DatabaseCollectionFixture.DatabaseCollection)]
    public class EventLogWriterTest
    {
        private const string TOKEN1_METADATA_URL = "http://example.com/token1";
        private const string TOKEN_1_ID = "token1";
        private const string TOKEN_2_ID = "token2";
        private const long ACCOUNT_1_ID = 1;
        private const string ACCOUNT_1_ADDR = "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P";

        private readonly GraphQlDbContextFactoryStub _dbContextFactory;
        private readonly EventLogWriter writer;
        private readonly IAccountLookup _accountLookup;

        public EventLogWriterTest(DatabaseFixture dbFixture)
        {
            _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
            _accountLookup = new AccountLookupStub();
            _accountLookup.AddToCache(AccountAddress.From(ACCOUNT_1_ADDR).GetBaseAddress().ToString(), ACCOUNT_1_ID);

            writer = new EventLogWriter(_dbContextFactory, _accountLookup, new NullMetrics());

            using var connection = dbFixture.GetOpenConnection();
            connection.Execute("TRUNCATE TABLE graphql_tokens");
            connection.Execute("TRUNCATE TABLE graphql_account_tokens");
        }

        [Fact]
        public void ShouldHandleTokenUpdates()
        {
            var updatesCount = writer.ApplyTokenUpdates(new List<CisEventTokenUpdate>() {
                new CisEventTokenAddedUpdate() { TokenId = TOKEN_1_ID, AmountDelta = 0, ContractIndex = 1, ContractSubIndex = 0 },
                new CisEventTokenAddedUpdate() { TokenId = TOKEN_2_ID, AmountDelta = 2, ContractIndex = 2, ContractSubIndex = 0 },
                new CisEventTokenAmountUpdate() { TokenId = TOKEN_1_ID, AmountDelta = 1, ContractIndex= 1, ContractSubIndex = 0 },
                new CisEventTokenAmountUpdate() { TokenId = TOKEN_2_ID, AmountDelta = -1, ContractIndex= 2, ContractSubIndex = 0 },
                new CisEventTokenMetadataUpdate() {
                        TokenId = TOKEN_1_ID,
                        MetadataUrl=TOKEN1_METADATA_URL,
                        ContractIndex= 1,
                        ContractSubIndex = 0
                    },
            });

            updatesCount.Should().Be(5);

            using var dbContext = _dbContextFactory.CreateDbContext();
            var allTokens = dbContext.Tokens.ToList();

            allTokens.Count.Should().Be(2);

            var token1s = allTokens.Where(t => t.ContractIndex == 1 && t.ContractSubIndex == 0 && t.TokenId == TOKEN_1_ID).ToList();
            token1s.Count.Should().Be(1);

            var token1 = token1s.Single();
            token1.ContractIndex.Should().Be(1);
            token1.TotalSupply.Should().Be(1);
            token1.ContractSubIndex.Should().Be(0);
            token1.MetadataUrl.Should().Be(TOKEN1_METADATA_URL);

            var token2s = allTokens.Where(t => t.ContractIndex == 2 && t.ContractSubIndex == 0 && t.TokenId == TOKEN_2_ID).ToList();
            token2s.Count.Should().Be(1);

            var token2 = token2s.Single();
            token2.ContractIndex.Should().Be(2);
            token2.TotalSupply.Should().Be(1);
            token2.ContractSubIndex.Should().Be(0);
            token2.MetadataUrl.Should().BeNull();
        }

        [Fact]
        public void ShouldHandleAccountUpdates()
        {
            var updatesCount = writer.ApplyAccountUpdates(new List<CisAccountUpdate>() {
                new CisAccountUpdate() { ContractIndex = 1, ContractSubIndex = 0, TokenId = TOKEN_1_ID, AmountDelta = 1, Address = AccountAddress.From(ACCOUNT_1_ADDR) },
                new CisAccountUpdate() { ContractIndex = 1, ContractSubIndex = 0, TokenId = TOKEN_1_ID, AmountDelta = 2, Address = AccountAddress.From(ACCOUNT_1_ADDR) },
                new CisAccountUpdate() { ContractIndex = 1, ContractSubIndex = 0, TokenId = TOKEN_1_ID, AmountDelta = -1, Address = AccountAddress.From(ACCOUNT_1_ADDR)},
            });

            updatesCount.Should().Be(3);

            using var dbContext = _dbContextFactory.CreateDbContext();
            var allAccntTokens = dbContext.AccountTokens.ToList();
            allAccntTokens.Count.Should().Be(1);

            var accntToken = allAccntTokens.Single();
            accntToken.AccountId.Should().Be(ACCOUNT_1_ID);
            accntToken.ContractIndex.Should().Be(1);
            accntToken.ContractSubIndex.Should().Be(0);
            accntToken.Balance.Should().Be(2);
        }
    }
}