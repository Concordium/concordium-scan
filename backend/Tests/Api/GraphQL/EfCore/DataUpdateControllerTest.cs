using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Stubs;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class DataUpdateControllerTest : IClassFixture<DatabaseFixture>
{
    private readonly DataUpdateController _target;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly BlockInfoBuilder _blockInfoBuilder = new();
    private readonly BlockSummaryBuilder _blockSummaryBuilder = new();
    private AccountInfo[] _createdAccounts = new AccountInfo[0];

    public DataUpdateControllerTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new DataUpdateController(_dbContextFactory, new TopicEventSenderStub(), 
            new BlockWriter(_dbContextFactory), new IdentityProviderWriter(_dbContextFactory), 
            new AccountReleaseScheduleWriter(dbFixture.DatabaseSettings));

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
        connection.Execute("TRUNCATE TABLE graphql_finalization_rewards");
        connection.Execute("TRUNCATE TABLE graphql_baking_rewards");
        connection.Execute("TRUNCATE TABLE graphql_finalization_summary_finalizers");
        connection.Execute("TRUNCATE TABLE graphql_transactions");
        connection.Execute("TRUNCATE TABLE graphql_transaction_events");
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }

    [Fact]
    public async Task Account_AccountCreated()
    {
        var slotTime = new DateTimeOffset(2020, 10, 01, 12, 0, 15, TimeSpan.Zero);
        
        _blockInfoBuilder.WithBlockSlotTime(slotTime);

        _createdAccounts = new [] {
            new AccountInfo { AccountAddress = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd") }};

        await WriteData();
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = await dbContext.Accounts.SingleAsync();
        account.Id.Should().BeGreaterThan(0);
        account.Address.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        account.CreatedAt.Should().Be(slotTime);
    }

    private async Task WriteData()
    {
        var blockInfo = _blockInfoBuilder.Build();
        var blockSummary = _blockSummaryBuilder.Build();
        await _target.BlockDataReceived(blockInfo, blockSummary, _createdAccounts, new RewardStatusBuilder().Build());
    }
}