using Application.Aggregates.Contract.Entities;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Aggregates.Contract.Entities;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class ContractJobTests
{
    private readonly DatabaseFixture _databaseFixture;

    public ContractJobTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task GivenTwoEntitiesWithSameJobName_WhenSave_ThenFailDueToUniquenessConstrain()
    {
        // Arrange
        var first = new ContractJob("name");
        var second = new ContractJob("name");

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