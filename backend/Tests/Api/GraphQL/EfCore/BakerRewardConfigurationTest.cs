using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class BakerRewardConfigurationTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);

    public BakerRewardConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
    }

    [Theory]
    [InlineData(AccountStatementEntryType.TransferIn, false)]
    [InlineData(AccountStatementEntryType.TransferOut, false)]
    [InlineData(AccountStatementEntryType.AmountDecrypted, false)]
    [InlineData(AccountStatementEntryType.AmountEncrypted, false)]
    [InlineData(AccountStatementEntryType.TransactionFee, false)]
    [InlineData(AccountStatementEntryType.FinalizationReward, true)]
    [InlineData(AccountStatementEntryType.FoundationReward, true)] 
    [InlineData(AccountStatementEntryType.BakerReward, true)] 
    [InlineData(AccountStatementEntryType.TransactionFeeReward, true)]
    public async Task FiltersByEntryType(AccountStatementEntryType entryType, bool expectedFound)
    {
        var input = new AccountStatementEntryBuilder()
            .WithEntryType(entryType)
            .Build();

        await AddAccountStatementEntry(input);
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.BakerRewards.AnyAsync();
        result.Should().Be(expectedFound);
    }

    [Theory]
    [InlineData(AccountStatementEntryType.FinalizationReward, BakerRewardType.FinalizationReward)]
    [InlineData(AccountStatementEntryType.FoundationReward, BakerRewardType.FoundationReward)]
    [InlineData(AccountStatementEntryType.BakerReward, BakerRewardType.BakerReward)]
    [InlineData(AccountStatementEntryType.TransactionFeeReward, BakerRewardType.TransactionFeeReward)]
    public async Task MappingOfFields(AccountStatementEntryType inputType, BakerRewardType expectedType)
    {
        var input = new AccountStatementEntryBuilder()
            .WithEntryType(inputType)
            .WithAccountId(42)
            .WithTimestamp(_anyDateTimeOffset)
            .WithAmount(3000)
            .WithBlockId(555)
            .Build();

        await AddAccountStatementEntry(input);
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.BakerRewards.SingleAsync();
        result.BakerId.Should().Be(42);
        result.Index.Should().Be(input.Index);
        result.Timestamp.Should().Be(_anyDateTimeOffset);
        result.RewardType.Should().Be(expectedType);
        result.Amount.Should().Be(3000);
        result.BlockId.Should().Be(555);

    }

    private async Task AddAccountStatementEntry(params AccountStatementEntry[] input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.AccountStatementEntries.AddRange(input);
        await dbContext.SaveChangesAsync();
    }
}