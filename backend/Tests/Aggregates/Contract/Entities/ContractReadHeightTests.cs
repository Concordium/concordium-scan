using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Aggregates.Contract.Entities;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class ContractReadHeightTests
{
    private readonly DatabaseFixture _databaseFixture;

    public ContractReadHeightTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task GivenTwoEntitiesWithSameHeight_WhenSave_ThenFailDueToUniquenessConstrain()
    {
        // Arrange
        var first = new ContractReadHeight(1, ImportSource.DatabaseImport);
        var second = new ContractReadHeight(1, ImportSource.NodeImport);

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