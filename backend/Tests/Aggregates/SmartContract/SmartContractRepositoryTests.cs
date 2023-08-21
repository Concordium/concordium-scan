using Application.Aggregates.SmartContract;
using Application.Api.GraphQL.EfCore;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;

namespace Tests.Aggregates.SmartContract;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class SmartContractRepositoryTests
{
    private readonly DbContextOptions<GraphQlDbContext> _dbContextOptions;


    public SmartContractRepositoryTests(DatabaseFixture databaseFixture)
    {
        _dbContextOptions = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(databaseFixture.DatabaseSettings.ConnectionString)
            .Options;
    }
    [Fact]
    public async Task GivenEntityWithBlockHeight_WhenGetReadOnlySmartContractReadHeightAtHeight_ThenReturnEntity()
    {
        // Arrange
        const ulong blockHeight = 42;
        await DeleteTables("graphql_smart_contract_read_heights");
        await using (var context = new GraphQlDbContext(_dbContextOptions))
        {
            await context.SmartContractReadHeights
                .AddAsync(new SmartContractReadHeight(blockHeight));
            await context.SaveChangesAsync();
        }
        var graphQlDbContext = new GraphQlDbContext(_dbContextOptions);
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlySmartContractReadHeightAtHeight(blockHeight);

        // Assert
        actual.Should().NotBeNull();
        actual!.BlockHeight.Should().Be(42);
    }
    
    [Fact]
    public async Task GivenNoEntityWithBlockHeight_WhenGetReadOnlySmartContractReadHeightAtHeight_ThenReturnNull()
    {
        // Arrange
        const ulong blockHeight = 42;
        await DeleteTables("graphql_smart_contract_read_heights");
        var graphQlDbContext = new GraphQlDbContext(_dbContextOptions);
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlySmartContractReadHeightAtHeight(blockHeight);

        // Assert
        actual.Should().BeNull();
    }

    private static async Task DeleteTables(params string[] tables)
    {
        await using var connection = DatabaseFixture.GetOpenConnection();
        foreach (var table in tables)
        {
            await connection.ExecuteAsync($"truncate table {table}");
        }
    }
}