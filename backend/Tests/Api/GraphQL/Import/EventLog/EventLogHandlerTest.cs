using System.Collections.Generic;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Import.EventLogs;
using Application.Api.GraphQL.Transactions;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using FluentAssertions;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import.EventLog;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class EventLogHandlerTest
{
    private readonly DatabaseFixture _fixture;

    public EventLogHandlerTest(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public Task GivenMintCisEvents_WhenParse_ThenSaveTokenEvent()
    {
        const string cisEvent = "fe040000000101009d230671ab6efaf2861f0b5942e650186036b8fbb4e9973f5634b43e664d3b4b";
        
        return GivenEvent_WhenParse_ThenSaveEvent(cisEvent, typeof(CisMintEvent));
    }
    
    [Fact]
    public Task GivenTransferCisEvents_WhenParse_ThenSaveTokenEvent()
    {
        const string cisEvent = "ff040000000101009d230671ab6efaf2861f0b5942e650186036b8fbb4e9973f5634b43e664d3b4b009a24cbfa7d436c36def76154006e20c30c1a8213d02ee7971f5f65cf1e4206e7";
        
        return GivenEvent_WhenParse_ThenSaveEvent(cisEvent, typeof(CisTransferEvent));
    }
    
    [Fact]
    public Task GivenTokenMetadataCisEvents_WhenParse_ThenSaveTokenEvent()
    {
        const string cisEvent = "fb0400000001540068747470733a2f2f697066732e696f2f697066732f516d563552454533484a524c5448646d71473138576335504246334e6339573564514c345270374d7842737838713f66696c656e616d653d6e66742e6a706700";
        
        return GivenEvent_WhenParse_ThenSaveEvent(cisEvent, typeof(CisTokenMetadataEvent));
    }
    
    [Fact]
    public Task GivenBurnCisEvents_WhenParse_ThenSaveTokenEvent()
    {
        const string cisEvent = "fd0080a094a58d1d00f761affb26ea6bbd14e4c50e51984d6d059156fa86658126c5ca0b747d60ba00";
        
        return GivenEvent_WhenParse_ThenSaveEvent(cisEvent, typeof(CisBurnEvent));
    }

    private async Task GivenEvent_WhenParse_ThenSaveEvent(string cisEventInHexadecimal, Type expectedCisEventType)
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_tokens", "graphql_account_tokens", "graphql_token_events");
        
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string name = "inventory.transfer";
        _ = ReceiveName.TryParse(name, out var output);
        
        var dbContractFactoryMock = _fixture.CreateDbContractFactoryMock();
        var accountLookup = new AccountLookupStub();
        accountLookup.AddToCache(AccountAddress.From("48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e").GetBaseAddress().ToString(), 1);
        accountLookup.AddToCache(AccountAddress.From("47da8rxVf4vFuF21hFypBJ3eGibxGSuricuAHnUpVbZjLeB4ML").GetBaseAddress().ToString(), 2);
        accountLookup.AddToCache(AccountAddress.From("4phD1qaS3U1nLrzJcgYyiPq1k8aV1wAjTjYVPE3JaqovViXS4j").GetBaseAddress().ToString(), 3);

        var eventLogWriter = new EventLogWriter(
            dbContractFactoryMock.Object, 
            accountLookup,
            Mock.Of<IMetrics>());
        var eventLogHandler = new EventLogHandler(eventLogWriter);
        var updated = new Updated(
            ContractVersion.V1,
            ContractAddress.From(index, subIndex),
            AccountAddress.From(address),
            CcdAmount.Zero,
            new Parameter(Array.Empty<byte>()), 
            output.ReceiveName!,
            new List<ContractEvent>
            {
                new(Convert.FromHexString(cisEventInHexadecimal))
            });
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{updated});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        var transactionPair = new TransactionPair(blockItemSummary, new Transaction());

        // Act
        eventLogHandler.HandleLogs(new []{transactionPair});

        // Assert
        await using var context = _fixture.CreateGraphQlDbContext();
        var tokenEvents = context.TokenEvents.ToList();
        tokenEvents.Count.Should().Be(1);
        tokenEvents[0].Event.Should().BeOfType(expectedCisEventType);
    }
}
