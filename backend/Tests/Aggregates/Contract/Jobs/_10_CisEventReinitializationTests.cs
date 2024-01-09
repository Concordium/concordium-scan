using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.EventLogs;
using Application.Aggregates.Contract.Jobs;
using Application.Api.GraphQL.Transactions;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using FluentAssertions;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Stubs;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class _10_CisEventReinitializationTests
{
    private readonly DatabaseFixture _fixture;
    private const int ContractIndex = 2059;
    
    public _10_CisEventReinitializationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task WhenRunJob_ThenUpdateTokenRelatedEntities()
    {
        // Assert
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        const int expectedBalance = 999958;
        const string expectedTokenId = "";
        const int accountId = 1;
        await AddContractEvents();
        var accountLookup = new AccountLookupStub();
        accountLookup.AddToCache(AccountAddress.From("3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV").GetBaseAddress().ToString(), accountId);
        
        var eventLogWriter = new EventLogWriter(
            _fixture.CreateDbContractFactoryMock().Object, 
            accountLookup,
            Mock.Of<IMetrics>());
        var eventLogHandler = new EventLogHandler(eventLogWriter, Mock.Of<ITopicEventSender>());

        var job = new _10_CisEventReinitialization(
            _fixture.CreateDbContractFactoryMock().Object,
            eventLogHandler,
            Options.Create(new ContractAggregateOptions())
            );

        // Act
        await job.Setup(CancellationToken.None);
        await job.Process(ContractIndex, CancellationToken.None);
        
        // Assert
        await using var context = _fixture.CreateGraphQlDbContext();
        
        var token = await context.Tokens.SingleAsync();
        token.TokenId.Should().Be(expectedTokenId);
        token.TotalSupply.Should().Be(expectedBalance);

        var tokenEvents = await context.TokenEvents.ToListAsync();
        tokenEvents.Count.Should().Be(2);

        var accountToken = await context.AccountTokens.SingleAsync();
        accountToken.TokenId.Should().Be(expectedTokenId);
        accountToken.ContractIndex.Should().Be(ContractIndex);
        accountToken.Balance.Should().Be(new BigInteger(expectedBalance));
        accountToken.AccountId.Should().Be(accountId);
    }
    
    private async Task AddContractEvents()
    {
        const string mintEvent = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        const string burnEvent = "fd002a005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        var contractAddress = ContractAddress.From(ContractIndex, 0);
        const string name = "inventory.transfer";
        _ = ReceiveName.TryParse(name, out var output);
        var mintUpdate = new Updated(
            ContractVersion.V1,
            contractAddress,
            AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
            CcdAmount.Zero,
            new Parameter(Array.Empty<byte>()), 
            output.ReceiveName!,
            new List<ContractEvent>
            {
                new(Convert.FromHexString(mintEvent))
            });
        var contractMintUpdate = ContractUpdated.From(mintUpdate);
        var contractMintEvent = ContractEventBuilder.Create()
            .WithContractAddress(new Application.Api.GraphQL.ContractAddress(contractAddress.Index, contractAddress.SubIndex))
            .WithEvent(contractMintUpdate)
            .WithBlockHeight(1)
            .Build();
        
        var burnUpdate = new Updated(
            ContractVersion.V1,
            contractAddress,
            AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
            CcdAmount.Zero,
            new Parameter(Array.Empty<byte>()), 
            output.ReceiveName!,
            new List<ContractEvent>
            {
                new(Convert.FromHexString(burnEvent))
            });
        var contractBurnUpdate = ContractUpdated.From(burnUpdate);
        var contractBurnEvent = ContractEventBuilder.Create()
            .WithContractAddress(new Application.Api.GraphQL.ContractAddress(contractAddress.Index, contractAddress.SubIndex))
            .WithEvent(contractBurnUpdate)
            .WithBlockHeight(2)
            .Build();
        
        await using var context = _fixture.CreateGraphQlDbContext();
        await context.AddRangeAsync(contractMintEvent, contractBurnEvent);
        await context.SaveChangesAsync();
    }    
}
