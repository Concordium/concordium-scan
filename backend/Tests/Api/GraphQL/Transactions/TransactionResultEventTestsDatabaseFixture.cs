using Application.Api.GraphQL;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Transactions;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class TransactionResultEventTestsDatabaseFixture
{
    private readonly DatabaseFixture _databaseFixture;
    
    public TransactionResultEventTestsDatabaseFixture(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ContractVersion.V0)]
    [InlineData(ContractVersion.V1)]
    public async Task GivenContractVersion_WhenFetchDataFromDatabase_ThenParse(ContractVersion? version)
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_transaction_events");

        var updated = TransactionResultEventStubs.ContractUpdated(version: version);

        await using var context = _databaseFixture.CreateGraphQlDbContext();
        await context.TransactionResultEvents
            .AddAsync(new TransactionRelated<TransactionResultEvent>(
                0,
                2,
                updated
            ));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var transactionEvent = await context.TransactionResultEvents.FirstOrDefaultAsync();
        
        // Assert
        transactionEvent.Should().NotBeNull();
        transactionEvent!.Entity.Should().BeOfType<ContractUpdated>();
        var contractUpdated = (transactionEvent.Entity as ContractUpdated)!;
        contractUpdated.ReceiveName.Should().Be(updated.ReceiveName);
        contractUpdated.Version.Should().Be(version);
    }
}
