using System.Threading;
using Application.Api.GraphQL.Transactions;
using Application.Database.MigrationJobs;
using Application.Import.ConcordiumNode;
using Application.Import.ConcordiumNode.ConcordiumClientWrappers;
using Application.Observability;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using AmountFraction = Concordium.Sdk.Types.AmountFraction;
using FinalizationCommitteeParameters = Concordium.Sdk.Types.FinalizationCommitteeParameters;
using FinalizationCommitteeParametersUpdate = Concordium.Sdk.Types.FinalizationCommitteeParametersUpdate;
using TransactionHash = Concordium.Sdk.Types.TransactionHash;
using UpdateType = Concordium.Sdk.Types.UpdateType;

namespace Tests.Database.MigrationJobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class _01_AddMissingChainUpdateEventsTests
{
    private readonly DatabaseFixture _fixture;

    public _01_AddMissingChainUpdateEventsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task WhenAddMissingEvents_ThenPresentInDatabaseAfterJobRun()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_main_migration_jobs");
        await DatabaseFixture.TruncateTables("graphql_blocks");
        await DatabaseFixture.TruncateTables("graphql_transactions");
        await DatabaseFixture.TruncateTables("graphql_transaction_events");
        // const int blockId = 42;
        var finalizationCommitteeParametersUpdate = new FinalizationCommitteeParametersUpdate(
            new FinalizationCommitteeParameters(
                1,2,AmountFraction.From(3))
            );
        var details = new UpdateDetailsBuilder(finalizationCommitteeParametersUpdate)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(details)
            .Build();

        var wrapper = new Mock<IBlockItemSummaryWrapper>();
        wrapper.Setup(w => w.GetFinalizedBlockItemSummary())
            .Returns(blockItemSummary);

        var clientMock = new Mock<IConcordiumNodeClient>();
        clientMock.Setup(c => c.GetBlockItemStatusAsync(
                It.IsAny<TransactionHash>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(wrapper.Object));
        
        var block = new BlockBuilder()
            .Build();
        await _fixture.AddAsync(block);
        var blockId = block.Id;

        var transaction = new TransactionBuilder()
            .WithId(0)
            .WithBlockId(blockId)
            .WithTransactionType(new UpdateTransaction{UpdateTransactionType = UpdateType.FinalizationCommitteeParametersUpdate})
            .Build();

        await _fixture.AddAsync(transaction);
        var job = new _01_AddMissingChainUpdateEvents(
            _fixture.CreateDbContractFactoryMock().Object,
            clientMock.Object,
            new JobHealthCheck(),
            Options.Create(new MainMigrationJobOptions()));
        
        // Act
        await job.StartImport(CancellationToken.None);
        
        // Assert
        await using var context = _fixture.CreateGraphQlDbContext();
        var transactionRelated = await context.TransactionResultEvents.SingleAsync();
        transactionRelated.Entity.Should().BeOfType<ChainUpdateEnqueued>();
        var chainUpdateEnqueued = transactionRelated.Entity as ChainUpdateEnqueued;
        chainUpdateEnqueued!.Payload.Should().BeOfType<Application.Api.GraphQL.Transactions.FinalizationCommitteeParametersUpdate>();
        var committeeParametersUpdate = chainUpdateEnqueued.Payload as Application.Api.GraphQL.Transactions.FinalizationCommitteeParametersUpdate;
        committeeParametersUpdate!.MaxFinalizers.Should()
            .Be(finalizationCommitteeParametersUpdate.FinalizationCommitteeParameters.MaxFinalizers);
        committeeParametersUpdate!.MinFinalizers.Should()
            .Be(finalizationCommitteeParametersUpdate.FinalizationCommitteeParameters.MinFinalizers);
        committeeParametersUpdate!.FinalizersRelativeStakeThreshold.Should()
            .Be(finalizationCommitteeParametersUpdate.FinalizationCommitteeParameters.FinalizersRelativeStakeThreshold.AsDecimal());
    }
}
