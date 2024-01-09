using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Extensions;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using static Tests.TestUtilities.Stubs.TransactionResultEventStubs;

namespace Tests.Aggregates.Contract;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class ContractRepositoryTests
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly Mock<IDbContextFactory<GraphQlDbContext>> _dbContractFactoryMock;

    public ContractRepositoryTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture; 
        _dbContractFactoryMock = _databaseFixture.CreateDbContractFactoryMock();
    }

    [Fact]
    public async Task WhenUsingDifferentFrameworks_ThenSaveInOneTransaction()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_read_heights");
        
        // Act
        await using (var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object))
        {
            await contractRepository.AddAsync(new ContractReadHeight(1, ImportSource.NodeImport));

            await using (var innerContext = _databaseFixture.CreateGraphQlDbContext())
            {
                var connection = innerContext.Database.GetDbConnection();
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "insert into graphql_contract_read_heights(block_height, source, created_at) values(2, 0, now())");
                await connection.CloseAsync();
            }

            await contractRepository.CommitAsync();
        };
        
        // Assert
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        graphQlDbContext.ContractReadHeights.Count().Should().Be(2);
    }

    [Fact]
    public async Task GivenException_WhenOnlySavedUsingOneFramework_ThenDoNotCommitToDatabase()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_read_heights");
        
        // Act
        try
        {
            await using (var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object))
            {
                await contractRepository.AddAsync(new ContractReadHeight(1, ImportSource.NodeImport));

                await using (var innerContext = _databaseFixture.CreateGraphQlDbContext())
                {
                    var connection = innerContext.Database.GetDbConnection();
                    await connection.OpenAsync();
                    await connection.ExecuteAsync(
                        "insert into graphql_contract_read_heights(block_height, source) values(1, 0)");
                    await connection.CloseAsync();
                }

                throw new Exception();
            };
        }
        catch (Exception)
        {
            // ignored
        }
        
        // Assert
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        graphQlDbContext.ContractReadHeights.Count().Should().Be(0);
    }

    [Fact]
    public async Task WhenGetContractEventsAddedInTransaction_ThenReturnContractEventsInAddedState()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        var first = ContractEventBuilder.Create()
            .WithContractAddress(new ContractAddress(1,0))
            .Build();
        await using (var context = _databaseFixture.CreateGraphQlDbContext())
        {
            await context.AddAsync(first);
            await context.SaveChangesAsync();
        }
        var second = ContractEventBuilder.Create()
            .WithContractAddress(new ContractAddress(2,0))
            .Build();
        var third = ContractEventBuilder.Create()
            .WithContractAddress(new ContractAddress(3,0))
            .Build();
        
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        await contractRepository.AddAsync(second, third);
        
        // Act
        var contractEventsAddedInTransaction = contractRepository.GetContractEventsAddedInTransaction()
            .ToList();
        
        // Assert
        contractEventsAddedInTransaction.Count.Should().Be(2);
        contractEventsAddedInTransaction.SingleOrDefault(ce => ce.ContractAddressIndex == 2).Should().NotBeNull();
        contractEventsAddedInTransaction.SingleOrDefault(ce => ce.ContractAddressIndex == 3).Should().NotBeNull();
    }

    [Fact]
    public async Task WhenCallFromBlockHeightRangeGetBlockHeightsRead_ThenReturnReadEvents()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_read_heights");
        await _databaseFixture.AddAsync(
            new ContractReadHeight(1, ImportSource.DatabaseImport),
            new ContractReadHeight(2, ImportSource.NodeImport),
            new ContractReadHeight(3, ImportSource.DatabaseImport),
            new ContractReadHeight(5, ImportSource.DatabaseImport),
            new ContractReadHeight(6, ImportSource.DatabaseImport),
            new ContractReadHeight(7, ImportSource.DatabaseImport));
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        
        // Act
        var readHeights = await contractRepository.FromBlockHeightRangeGetBlockHeightsReadOrdered(2, 6);

        // Assert
        readHeights.Should().BeEquivalentTo(new List<ulong> { 2, 3, 5, 6 });
    }

    #region FromBlockHeightRangeGetContractRelatedRejections

    [Fact]
    public async Task WhenFromBlockHeightRangeGetContractRelatedRejections_ThenRelatedRejectedEvents()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_blocks");
        await DatabaseFixture.TruncateTables("graphql_transactions");
        var blockIds = await InsertFiveBlocks();
        await InsertSixTransactionsWithRejections(blockIds);
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        ContractExtensions.AddDapperTypeHandlers();
        
        // Act
        var events = await contractRepository.FromBlockHeightRangeGetContractRelatedRejections(1, 4);
        
        // Assert
        events.Count.Should().Be(4);
        var firstEvent = events[0];
        firstEvent.RejectedEvent.Should().BeOfType<InvalidInitMethod>();
        var secondEvent = events[1];
        secondEvent.RejectedEvent.Should().BeOfType<InvalidReceiveMethod>();
        var thirdEvent = events[2];
        thirdEvent.RejectedEvent.Should().BeOfType<ModuleHashAlreadyExists>();
        var fourth = events[3];
        fourth.RejectedEvent.Should().BeOfType<RejectedReceive>();
    }
    
    private async Task InsertSixTransactionsWithRejections(IList<long> blockIds)
    {
        var randoms = GetRandomsNotInList(1, blockIds);
        // Points to blocks within block height range and relevant
        var first = new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("1")
            .WithBlockId(blockIds[0])
            .WithRejectReason(new InvalidInitMethod("", ""))
            .Build();
        var second = new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("2")
            .WithBlockId(blockIds[1])
            .WithRejectReason(new InvalidReceiveMethod("", ""))
            .Build();
        var third = new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("3")
            .WithBlockId(blockIds[2])
            .WithRejectReason(new ModuleHashAlreadyExists(""))
            .Build();
        var fourth = new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("4")
            .WithBlockId(blockIds[3])
            .WithRejectReason(new RejectedReceive(1, new ContractAddress(1,0), "", ""))
            .Build();
        // Valid event but outside block range
        var outside = new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("5")
            .WithBlockId(randoms[0])
            .WithRejectReason(new ModuleHashAlreadyExists(""))
            .Build();
        // Not valid event and inside block height range
        var notValid = new TransactionBuilder()
            .WithId(0)
            .WithTransactionHash("6")
            .WithBlockId(blockIds[4])
            .WithRejectReason(new InvalidProof())
            .Build();
        await _databaseFixture.AddAsync(first, second, third, fourth);
        await _databaseFixture.AddAsync(outside);
        await _databaseFixture.AddAsync(notValid);
    }

    #endregion
    
    #region Test WhenGetContractRelatedBlockTransactionResultEventRelationsFromBlockHeightRange_ThenReturnEvents

    [Fact]
    public async Task WhenCallFromBlockHeightRangeGetContractRelatedTransactionResultEventRelations_ThenReturnEvents()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_blocks");
        await DatabaseFixture.TruncateTables("graphql_transactions");
        await DatabaseFixture.TruncateTables("graphql_transaction_events");
        var blockIds = await InsertFiveBlocks();
        var transactionIds = await InsertSixTransactions(blockIds);
        await InsertTransactionResultEvents(transactionIds);
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        ContractExtensions.AddDapperTypeHandlers();
        
        // Act
        var events = await contractRepository.FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(1, 4);
        
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
    public async Task GivenEntities_WhenGetReadOnlyLatestContractReadHeight_ThenReturnLatest()
    {
        // Arrange
        const ulong latestHeight = 3;
        await DatabaseFixture.TruncateTables("graphql_contract_read_heights");
        await _databaseFixture.AddAsync(
            new ContractReadHeight(1, ImportSource.DatabaseImport),
            new ContractReadHeight(latestHeight, ImportSource.NodeImport),
            new ContractReadHeight(2, ImportSource.DatabaseImport));
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        
        // Act
        var latest = await contractRepository.GetReadonlyLatestContractReadHeight();

        // Assert
        latest.Should().NotBeNull();
        latest!.BlockHeight.Should().Be(latestHeight);
        latest.Source.Should().Be(ImportSource.NodeImport);
    }
    
    [Fact]
    public async Task GivenNoEntities_WhenGetReadOnlyLatestContractReadHeight_ThenReturnNull()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_read_heights");
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        
        // Act
        var latest = await contractRepository.GetReadonlyLatestContractReadHeight();

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
        
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        
        // Act
        var latest = await contractRepository.GetReadonlyLatestImportState(CancellationToken.None);

        // Assert
        latest.Should().Be(maxBlockHeight);
    }
    
    [Fact]
    public async Task GivenNoEntities_WhenGetLatestImportState_ThenReturnZero()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_import_state");
        await using var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        
        // Act
        var latest = await contractRepository.GetReadonlyLatestImportState(CancellationToken.None);

        // Assert
        latest.Should().Be(0);
    }

    [Fact]
    public async Task WhenAddAsync_ThenSave()
    {
        // Arrange
        const ulong blockHeight = 42;
        const ImportSource source = ImportSource.DatabaseImport;
        await DatabaseFixture.TruncateTables("graphql_contract_read_heights");
        
        // Act
        await using (var contractRepository = await ContractRepository.Create(_dbContractFactoryMock.Object))
        {
            await contractRepository.AddAsync(new ContractReadHeight(blockHeight, source));
            await contractRepository.CommitAsync();    
        };
        
        // Assert
        var graphQlDbContext = _databaseFixture.CreateGraphQlDbContext();
        var contractReadHeight = await graphQlDbContext.ContractReadHeights
            .FirstOrDefaultAsync();
        contractReadHeight.Should().NotBeNull();
        contractReadHeight!.BlockHeight.Should().Be(blockHeight);
        contractReadHeight.Source.Should().Be(source);
    }

    [Fact]
    public async Task GivenContractInitializedEventInDatabase_WhenGetReadonlyContractInitializedEventAsync_ThenReturnCorrect()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        ContractExtensions.AddDapperTypeHandlers();
        var context = _databaseFixture.CreateGraphQlDbContext();
        var oneContract = new ContractAddress(1, 0);
        const string oneContractName = "init_foo";
        const string otherContractName = "init_bar";
        var otherContract = new ContractAddress(2,0);

        var first = ContractEventBuilder.Create()
            .WithContractAddress(oneContract)
            .WithBlockHeight(1)
            .WithEvent(new ContractInitialized("", oneContract, 10, oneContractName, ContractVersion.V0, Array.Empty<string>()))
            .Build();
        var second = ContractEventBuilder.Create()
            .WithContractAddress(oneContract)
            .WithBlockHeight(2)
            .WithEvent(new Transferred(2, oneContract, new AccountAddress("")))
            .Build();
        var third = ContractEventBuilder.Create()
            .WithContractAddress(otherContract)
            .WithBlockHeight(3)
            .WithEvent(new ContractInitialized("", otherContract, 10, otherContractName, ContractVersion.V0, Array.Empty<string>()))
            .Build();
        await context.AddRangeAsync(first, second, third);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using var repository = await ContractRepository.Create(_dbContractFactoryMock.Object);

        // Act
        var initialized = await repository.GetReadonlyContractInitializedEventAsync(oneContract);

        // Assert
        initialized.InitName.Should().Be(oneContractName);
    }
    
    [Fact]
    public async Task GivenContractInitializedEventInChangeTracker_WhenGetReadonlyContractInitializedEventAsync_ThenReturnCorrect()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        ContractExtensions.AddDapperTypeHandlers();
        var oneContract = new ContractAddress(1, 0);
        const string oneContractName = "init_foo";
        const string otherContractName = "init_bar";
        var otherContract = new ContractAddress(2,0);
        
        var second = ContractEventBuilder.Create()
            .WithBlockHeight(1)
            .WithEvent(new Transferred(2, oneContract, new AccountAddress("")))
            .Build();
        var third = ContractEventBuilder.Create()
            .WithBlockHeight(2)
            .WithEvent(new ContractInitialized("", otherContract, 10, otherContractName, ContractVersion.V0, Array.Empty<string>()))
            .Build();
        await using (var context = _databaseFixture.CreateGraphQlDbContext())
        {
            await context.AddRangeAsync(second, third);
            await context.SaveChangesAsync();    
        }
        
        await using var repository = await ContractRepository.Create(_dbContractFactoryMock.Object);
        var first = ContractEventBuilder.Create()
            .WithBlockHeight(3)
            .WithEvent(new ContractInitialized("", oneContract, 10, oneContractName, ContractVersion.V0, Array.Empty<string>()))
            .Build();
        await repository.AddAsync(first);

        // Act
        var initialized = await repository.GetReadonlyContractInitializedEventAsync(oneContract);

        // Assert
        initialized.InitName.Should().Be(oneContractName);
    }
}
