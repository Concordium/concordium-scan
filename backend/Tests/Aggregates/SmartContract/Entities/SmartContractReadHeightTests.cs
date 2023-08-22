using Application.Aggregates.SmartContract.Entities;
using Application.Aggregates.SmartContract.Types;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Aggregates.SmartContract.Entities;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class SmartContractReadHeightTests
{
    private readonly DatabaseFixture _databaseFixture;

    public SmartContractReadHeightTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task GivenTwoEntitiesWithSameHeight_WhenSave_ThenFailDueToUniquenessConstrain()
    {
        // Arrange
        var first = new SmartContractReadHeight(1, ImportSource.DatabaseImport);
        var second = new SmartContractReadHeight(1, ImportSource.NodeImport);

        await using var context = _databaseFixture.CreateGraphQlDbContext();
        await context.AddAsync(first);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Act
        await context.AddAsync(second);
        var action = async () => await context.SaveChangesAsync();
        
        // Assert
        await action.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }
    
}