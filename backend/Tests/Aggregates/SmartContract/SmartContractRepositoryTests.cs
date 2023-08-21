using System.Threading;
using Application.Aggregates.SmartContract;
using Application.Aggregates.SmartContract.Entities;
using Application.Aggregates.SmartContract.Types;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Aggregates.SmartContract;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class SmartContractRepositoryTests
{
    private readonly DatabaseFixture _databaseFixture;

    public SmartContractRepositoryTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task GivenEntityWithBlockHeight_WhenGetReadOnlySmartContractReadHeightAtHeight_ThenReturnEntity()
    {
        // Arrange
        const ulong blockHeight = 42;
        const ImportSource source = ImportSource.DatabaseImport;
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        await _databaseFixture.AddAsync(new SmartContractReadHeight(blockHeight, source));
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlySmartContractReadHeightAtHeight(blockHeight);

        // Assert
        actual.Should().NotBeNull();
        actual!.BlockHeight.Should().Be(42);
        actual!.Source.Should().Be(source);
    }
    
    [Fact]
    public async Task GivenNoEntityWithBlockHeight_WhenGetReadOnlySmartContractReadHeightAtHeight_ThenReturnNull()
    {
        // Arrange
        const ulong blockHeight = 42;
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlySmartContractReadHeightAtHeight(blockHeight);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GivenBlockAtHeight_WhenGetReadOnlyBlockIdAtHeight_ThenReturnBlockId()
    {
        // Arrange
        const int blockHeight = 42;
        await DatabaseFixture.TruncateTables("graphql_blocks");
        var block = new BlockBuilder()
            .WithBlockHeight(blockHeight)
            .Build();
        await _databaseFixture.AddAsync(block);
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var single = await graphQlDbContext.Blocks.SingleAsync();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlyBlockIdAtHeight(blockHeight);

        // Assert
        actual!.Should().Be(single.Id);
    }
    
    [Fact]
    public async Task GivenNoBlockAtHeight_WhenGetReadOnlyBlockIdAtHeight_ThenReturnDefault()
    {
        // Arrange
        const int blockHeight = 42;
        await DatabaseFixture.TruncateTables("graphql_blocks");
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var action = async () => await smartContractRepository.GetReadOnlyBlockIdAtHeight(blockHeight);

        // Assert
        await action.Should().ThrowAsync<System.InvalidOperationException>();
    }
    
    [Fact]
    public async Task GivenTransactionAtBlockId_WhenGetReadOnlyTransactionsAtBlockId_ThenReturnTransactions()
    {
        // Arrange
        const long blockId = 42;
        await DatabaseFixture.TruncateTables("graphql_transactions");
        var transaction = new TransactionBuilder()
            .WithId(0)
            .WithBlockId(blockId)
            .Build();
        await _databaseFixture.AddAsync(transaction);
        
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlyTransactionsAtBlockId(blockId);

        // Assert
        actual.Count.Should().Be(1);
        actual[0].BlockId.Should().Be(blockId);
    }
    
    [Fact]
    public async Task GivenNoTransactionAtBlockId_WhenGetReadOnlyTransactionsAtBlockId_ThenReturnEmptyList()
    {
        // Arrange
        const long blockId = 42;
        await DatabaseFixture.TruncateTables("graphql_transactions");

        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlyTransactionsAtBlockId(blockId);

        // Assert
        actual.Count.Should().Be(0);
    }
    
    [Fact]
    public async Task GivenTransactionResultEventAtTransactionId_WhenGetReadOnlyTransactionResultEventsFromTransactionId_ThenReturnTransactionResultEvents()
    {
        // Arrange
        const long transactionId = 42;
        await DatabaseFixture.TruncateTables("graphql_transaction_events");
        var transactionRelated = new TransactionRelated<TransactionResultEvent>(transactionId, 2, new TransferMemo("foo"));
        var transactionRelatedOther = new TransactionRelated<TransactionResultEvent>(12, 2, new TransferMemo("foo"));
        await _databaseFixture.AddAsync(transactionRelated, transactionRelatedOther);
        
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlyTransactionResultEventsFromTransactionId(transactionId);

        // Assert
        actual.Count.Should().Be(1);
        actual[0].TransactionId.Should().Be(transactionId);
    }

    [Fact]
    public async Task GivenEntities_WhenGetReadOnlyLatestSmartContractReadHeight_ThenReturnLatest()
    {
        // Arrange
        const ulong latestHeight = 3;
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        await _databaseFixture.AddAsync(
            new SmartContractReadHeight(1, ImportSource.DatabaseImport),
            new SmartContractReadHeight(latestHeight, ImportSource.NodeImport),
            new SmartContractReadHeight(2, ImportSource.DatabaseImport));
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetReadOnlyLatestSmartContractReadHeight();

        // Assert
        latest.Should().NotBeNull();
        latest!.BlockHeight.Should().Be(latestHeight);
        latest.Source.Should().Be(ImportSource.NodeImport);
    }
    
    [Fact]
    public async Task GivenNoEntities_WhenGetReadOnlyLatestSmartContractReadHeight_ThenReturnNull()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetReadOnlyLatestSmartContractReadHeight();

        // Assert
        latest.Should().BeNull();
    }
    
    [Fact]
    public async Task GivenEntities_WhenGetLatestImportState_ThenReturnLatest()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_import_state");
        var latestBlockSlotTime = new DateTime(2023, 08, 21, 0, 0, 0, DateTimeKind.Utc);
        const int maxBlockHeight = 42;
        var first = new ImportStateBuilder()
            .WithLastBlockSlotTime(latestBlockSlotTime)
            .WithMaxImportedBlockHeight(maxBlockHeight)
            .Build();
        var second = new ImportStateBuilder()
            .WithLastBlockSlotTime(latestBlockSlotTime.Subtract(TimeSpan.FromSeconds(1)))
            .WithMaxImportedBlockHeight(maxBlockHeight-1)
            .Build();
        var third = new ImportStateBuilder()
            .WithLastBlockSlotTime(latestBlockSlotTime.Subtract(TimeSpan.FromSeconds(2)))
            .WithMaxImportedBlockHeight(maxBlockHeight-2)
            .Build();
        await _databaseFixture.AddAsync(first, second, third);
        
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetLatestImportState(CancellationToken.None);

        // Assert
        latest.Should().Be(maxBlockHeight);
    }
    
    [Fact]
    public async Task GivenNoEntities_WhenGetLatestImportState_ThenReturnZero()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_import_state");
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetLatestImportState(CancellationToken.None);

        // Assert
        latest.Should().Be(0);
    }

    [Fact]
    public async Task WhenAddAsync_ThenSave()
    {
        // Arrange
        const ulong blockHeight = 42;
        const ImportSource source = ImportSource.DatabaseImport;
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new SmartContractRepository(graphQlDbContext);

        // Act
        await smartContractRepository.AddAsync(new SmartContractReadHeight(blockHeight, source));
        await smartContractRepository.SaveChangesAsync();
        graphQlDbContext.ChangeTracker.Clear();
        
        // Assert
        var smartContractReadHeight = await graphQlDbContext.SmartContractReadHeights
            .FirstOrDefaultAsync();
        smartContractReadHeight.Should().NotBeNull();
        smartContractReadHeight!.BlockHeight.Should().Be(blockHeight);
        smartContractReadHeight.Source.Should().Be(source);
    }
}