using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class ImportStateReaderTest : IClassFixture<DatabaseFixture>
{
    private readonly ImportStateReader _target;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public ImportStateReaderTest(DatabaseFixture dbFixture)
    {
        _target = new ImportStateReader(dbFixture.DatabaseSettings);
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
    }

    [Fact]
    public async Task ReadImportStatus_NoBlocksExist()
    {
        var result = await _target.ReadImportStatus();
        result.Should().NotBeNull();
        result.GenesisBlockHash.Should().BeNull();
        result.MaxBlockHeight.Should().BeNull();
    }
    
    [Fact]
    public async Task ReadImportStatus_BlocksExist()
    {
        var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Blocks.Add(new BlockBuilder().WithBlockHeight(0).WithBlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1").Build());
        dbContext.Blocks.Add(new BlockBuilder().WithBlockHeight(1).WithBlockHash("a9aaba2499bdc9e41c60a6e7d219f7be90bc68613715407989bc27da3fdcd62c").Build());
        dbContext.Blocks.Add(new BlockBuilder().WithBlockHeight(2).WithBlockHash("74cf5c60bea742c00d3cf6e8f3b71d1a475f227c866926b0d7f240097c7fb072").Build());
        dbContext.Blocks.Add(new BlockBuilder().WithBlockHeight(3).WithBlockHash("bc6c1654d7b851c50f8c6e36e8023d61337e29576ee5cd09df17585db2240fab").Build());
        await dbContext.SaveChangesAsync();

        var result = await _target.ReadImportStatus();
        result.Should().NotBeNull();
        result.GenesisBlockHash.Should().Be(new BlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1"));
        result.MaxBlockHeight.Should().Be(3);
    }
    
    [Fact]
    public async Task ReadImportStatus_BlocksExistButNotAtBlockHeightZero()
    {
        var block = new BlockBuilder().WithBlockHeight(1).Build();
        var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Blocks.Add(block);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _target.ReadImportStatus());
    }
}