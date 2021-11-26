using Application.Persistence;
using Dapper;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests;

public class BlockRepositoryTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly BlockRepository _target;

    public BlockRepositoryTest(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _target = new BlockRepository(_dbFixture.DatabaseSettings);
        
        using var connection = _dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE block");
    }

    [Fact]
    public void Insert()
    {
        var blockInfo = new BlockInfoBuilder().Build();
        _target.Insert(blockInfo, "{\"foo\": \"bar\"}");
    }

    [Fact]
    public void GetMaxBlockHeight_NoBlocksExist()
    {
        var result = _target.GetMaxBlockHeight();
        Assert.False(result.HasValue);
    }
    
    [Fact]
    public void GetMaxBlockHeight_BlocksExist()
    {
        _target.Insert(new BlockInfoBuilder().WithBlockHeight(1).Build(), "{\"foo\": \"bar\"}");
        _target.Insert(new BlockInfoBuilder().WithBlockHeight(3).Build(), "{\"foo\": \"bar\"}");
        _target.Insert(new BlockInfoBuilder().WithBlockHeight(2).Build(), "{\"foo\": \"bar\"}");

        var result = _target.GetMaxBlockHeight();
        Assert.Equal(3, result);
    }
}