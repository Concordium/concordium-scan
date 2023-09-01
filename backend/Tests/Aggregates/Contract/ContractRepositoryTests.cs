using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Extensions;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using static Tests.TestUtilities.Stubs.TransactionResultEventStubs;

namespace Tests.Aggregates.Contract;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class ContractRepositoryTests
{
    private readonly DatabaseFixture _databaseFixture;

    public ContractRepositoryTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task WhenCallFromBlockHeightRangeGetBlockHeightsRead_ThenReturnReadEvents()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        await _databaseFixture.AddAsync(
            new ContractReadHeight(1, ImportSource.DatabaseImport),
            new ContractReadHeight(2, ImportSource.NodeImport),
            new ContractReadHeight(3, ImportSource.DatabaseImport),
            new ContractReadHeight(5, ImportSource.DatabaseImport),
            new ContractReadHeight(6, ImportSource.DatabaseImport),
            new ContractReadHeight(7, ImportSource.DatabaseImport));
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new ContractRepository(graphQlDbContext);
        
        // Act
        var readHeights = await smartContractRepository.FromBlockHeightRangeGetBlockHeightsReadOrdered(2, 6);

        // Assert
        readHeights.Should().BeEquivalentTo(new List<ulong> { 2, 3, 5, 6 });
    }
    

    #region Test WhenGetSmartContractRelatedBlockTransactionResultEventRelationsFromBlockHeightRange_ThenReturnEvents

    [Fact]
    public async Task WhenCallFromBlockHeightRangeGetSmartContractRelatedTransactionResultEventRelations_ThenReturnEvents()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_smart_contracts");
        await DatabaseFixture.TruncateTables("graphql_smart_contract_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_smart_contract_link_events");
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        await DatabaseFixture.TruncateTables("graphql_blocks");
        await DatabaseFixture.TruncateTables("graphql_transactions");
        await DatabaseFixture.TruncateTables("graphql_transaction_events");
        var blockIds = await InsertFiveBlocks();
        var transactionIds = await InsertSixTransactions(blockIds);
        await InsertTransactionResultEvents(transactionIds);
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new ContractRepository(graphQlDbContext);
        ContractExtensions.AddDapperTypeHandlers();
        
        // Act
        var events = await smartContractRepository.FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(1, 4);
        
        // Assert
        events.Count.Should().Be(3);
        
        // First event
        var firstEvent = events[0];
        firstEvent.BlockHeight.Should().Be(1);
        firstEvent.TransactionType.Should().BeOfType<AccountTransaction>();
        firstEvent.TransactionSender.Should().NotBeNull();
        firstEvent.TransactionHash.Should().NotBeNull();
        firstEvent.Event.Should().BeOfType<ContractUpdated>();
        var firstEventEvent = (firstEvent.Event as ContractUpdated)!;
        firstEventEvent.ContractAddress.Index.Should().Be(1);
        
        // Second event
        var secondEvent = events[1];
        secondEvent.BlockHeight.Should().Be(1);
        secondEvent.TransactionType.Should().BeOfType<AccountTransaction>();
        secondEvent.TransactionSender.Should().NotBeNull();
        secondEvent.TransactionHash.Should().NotBeNull();
        secondEvent.Event.Should().BeOfType<ContractUpdated>();
        var secondEventEvent = (secondEvent.Event as ContractUpdated)!;
        secondEventEvent.ContractAddress.Index.Should().Be(2);
        
        // Third event
        var thirdEvent = events[2];
        thirdEvent.BlockHeight.Should().Be(2);
        thirdEvent.TransactionType.Should().BeOfType<AccountTransaction>();
        thirdEvent.TransactionSender.Should().NotBeNull();
        thirdEvent.TransactionHash.Should().NotBeNull();
        thirdEvent.Event.Should().BeOfType<ContractUpdated>();
        var thirdEventEvent = (thirdEvent.Event as ContractUpdated)!;
        thirdEventEvent.ContractAddress.Index.Should().Be(4);
    }
    
    /// <summary>
    /// Generates three transaction result event relevant for smart contracts and which is within
    /// block height range 1-4.
    /// </summary>
    /// <param name="transactionIds"></param>
    private async Task InsertTransactionResultEvents(IList<long> transactionIds)
    {
        var randoms = GetRandomsNotInList(2, transactionIds);
        // Points to transactions within block height range
        var first = new TransactionRelated<TransactionResultEvent>(transactionIds[0], 1, ContractUpdated(1,2));
        var second = new TransactionRelated<TransactionResultEvent>(transactionIds[1], 1, ContractUpdated(2, 2));
        var third = new TransactionRelated<TransactionResultEvent>(transactionIds[2], 1, ContractUpdated(4,2));
        // Points to transaction outside block height range (points to block with height 5)
        var fourth = new TransactionRelated<TransactionResultEvent>(transactionIds[5], 1, ContractUpdated(3,2));
        // Points to some randoms transaction id's which doesn't exist
        var fifth = new TransactionRelated<TransactionResultEvent>(randoms[1], 1, ContractUpdated(5,2));
        // Non relevant events
        var transactionRelated = new TransactionRelated<TransactionResultEvent>(transactionIds[3], 2, new TransferMemo("foo"));
        var transactionRelatedOther = new TransactionRelated<TransactionResultEvent>(randoms[0], 2, new TransferMemo("foo"));
        await _databaseFixture.AddAsync(first, second, third, fourth, fifth);
        await _databaseFixture.AddAsync(transactionRelated);
        await _databaseFixture.AddAsync(transactionRelatedOther);
    }

    private static IList<long> GetRandomsNotInList(int count, IList<long> input)
    {
        var found = 0;
        var randoms = new List<long>();
        while (found < count)
        {
            var next = Random.Shared.Next(99, 99_999);
            if (randoms.Contains(next) || input.Contains(next))
            {
                continue;
            }
            randoms.Add(next);
            found++;
        }
        return randoms;
    }

    private async Task<IList<long>> InsertSixTransactions(IList<long> blockIds)
    {
        await using var context = _databaseFixture.CreateGraphQlDbContext();
        var transactions = new List<Transaction>
        {
            new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("first")
            .WithBlockId(blockIds[0])
            .Build()
        };
        for (var i = 0; i < 5; i++)
        {
            var transaction = new TransactionBuilder()
                .WithBlockId(blockIds[i])
                .WithTransactionHash($"{i}")
                .WithId(0)
                .Build();
            transactions.Add(transaction);    
        }
        await context.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
        return transactions
            .Select(t => t.Id)
            .ToList();
    }

    private async Task<IList<long>> InsertFiveBlocks()
    {
        await using var context = _databaseFixture.CreateGraphQlDbContext();
        var blocks = new List<Block>();
        for (var i = 1; i <= 5; i++)
        {
            var block = new BlockBuilder()
                .WithId(0)
                .WithBlockHeight(i)
                .Build();
            blocks.Add(block);
        }
        await context.AddRangeAsync(blocks);
        await context.SaveChangesAsync();
        return blocks
            .Select(b => b.Id)
            .ToList();
    }

    #endregion

    [Fact]
    public async Task GivenEntityWithBlockHeight_WhenGetReadOnlySmartContractReadHeightAtHeight_ThenReturnEntity()
    {
        // Arrange
        const ulong blockHeight = 42;
        const ImportSource source = ImportSource.DatabaseImport;
        await DatabaseFixture.TruncateTables("graphql_smart_contract_read_heights");
        await _databaseFixture.AddAsync(new ContractReadHeight(blockHeight, source));
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new ContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlyContractReadHeightAtHeight(blockHeight);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

        // Act
        var actual = await smartContractRepository.GetReadOnlyContractReadHeightAtHeight(blockHeight);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

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
            new ContractReadHeight(1, ImportSource.DatabaseImport),
            new ContractReadHeight(latestHeight, ImportSource.NodeImport),
            new ContractReadHeight(2, ImportSource.DatabaseImport));
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new ContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetReadOnlyLatestContractReadHeight();

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetReadOnlyLatestContractReadHeight();

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetReadOnlyLatestImportState(CancellationToken.None);

        // Assert
        latest.Should().Be(maxBlockHeight);
    }
    
    [Fact]
    public async Task GivenNoEntities_WhenGetLatestImportState_ThenReturnZero()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_import_state");
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var smartContractRepository = new ContractRepository(graphQlDbContext);
        
        // Act
        var latest = await smartContractRepository.GetReadOnlyLatestImportState(CancellationToken.None);

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
        var smartContractRepository = new ContractRepository(graphQlDbContext);

        // Act
        await smartContractRepository.AddAsync(new ContractReadHeight(blockHeight, source));
        await smartContractRepository.SaveChangesAsync();
        graphQlDbContext.ChangeTracker.Clear();
        
        // Assert
        var smartContractReadHeight = await graphQlDbContext.ContractReadHeights
            .FirstOrDefaultAsync();
        smartContractReadHeight.Should().NotBeNull();
        smartContractReadHeight!.BlockHeight.Should().Be(blockHeight);
        smartContractReadHeight.Source.Should().Be(source);
    }
}