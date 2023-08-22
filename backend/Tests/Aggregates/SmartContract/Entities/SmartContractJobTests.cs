using Application.Aggregates.SmartContract.Entities;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Aggregates.SmartContract.Entities;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class SmartContractJobTests
{
    private readonly DatabaseFixture _databaseFixture;

    public SmartContractJobTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task GivenTwoEntitiesWithSameJobName_WhenSave_ThenFailDueToUniquenessConstrain()
    {
        // Arrange
        var first = new SmartContractJob("name");
        var second = new SmartContractJob("name");

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