using System.Collections.Generic;
using System.Threading;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Extensions;
using Application.Api.GraphQL.Import;
using Application.Import;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using AccountAddress = Concordium.Sdk.Types.AccountAddress;
using AccountIndex = Concordium.Sdk.Types.AccountIndex;
using BakerId = Concordium.Sdk.Types.BakerId;
using BakerKeysEvent = Concordium.Sdk.Types.BakerKeysEvent;
using BakerPoolInfo = Concordium.Sdk.Types.BakerPoolInfo;
using BakerPoolOpenStatus = Concordium.Sdk.Types.BakerPoolOpenStatus;
using BlockItemSummary = Concordium.Sdk.Types.BlockItemSummary;
using ChainParametersV1Builder = Tests.TestUtilities.Builders.GraphQL.ChainParametersV1Builder;
using ChainParametersV2 = Application.Api.GraphQL.ChainParametersV2;
using CommissionRates = Concordium.Sdk.Types.CommissionRates;
using ProtocolVersion = Concordium.Sdk.Types.ProtocolVersion;
using TransactionHash = Concordium.Sdk.Types.TransactionHash;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class BakerImportHandlerTest
{
    private readonly DatabaseFixture _dbFixture;
    
    public BakerImportHandlerTest(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task WhenUpdateCurrentPaydayStatusOnAllBakers_ThenBakersUpdates()
    {
        // Arrange
        TruncateTables();
        var beforeStatus = new CurrentPaydayBakerPoolStatus(
            0,
            false,
            CcdAmount.Zero,
            CcdAmount.FromMicroCcd(1),
            0.2m,
            CcdAmount.FromMicroCcd(3),
            CcdAmount.FromMicroCcd(4));
        var beforeRates = new CommissionRates(
            AmountFraction.From(0.1m),
            AmountFraction.From(0.2m),
            AmountFraction.From(0.3m)
        );
        var currentPaydayStatus = new CurrentPaydayStatus(
            beforeStatus,
            beforeRates
        );
        var afterStatus = new CurrentPaydayBakerPoolStatus(
            0,
            false,
            CcdAmount.Zero,
            CcdAmount.FromMicroCcd(11),
            0.12m,
            CcdAmount.FromMicroCcd(13),
            CcdAmount.FromMicroCcd(14));
        var afterRates = new CommissionRates(
            AmountFraction.From(0.11m),
            AmountFraction.From(0.12m),
            AmountFraction.From(0.13m)
        );
        var factoryMock = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_dbFixture.CreateGraphQlDbContext()));
        var target = new BakerImportHandler(factoryMock.Object, new NullMetrics());
        var bakerPool = new BakerPoolBuilder()
            .WithPaydayStatus(currentPaydayStatus)
            .Build();
        var activeBakerState = new ActiveBakerStateBuilder()
            .WithPool(bakerPool)
            .Build();
        var build = new BakerBuilder()
            .WithState(activeBakerState)
            .Build();
        var graphQlDbContext = _dbFixture.CreateGraphQlDbContext();
        var entityEntry = await graphQlDbContext.Bakers.AddAsync(build);
        await graphQlDbContext.SaveChangesAsync();
        graphQlDbContext.ChangeTracker.Clear();
        var bakerPoolStatus = new BakerPoolStatus(
            new BakerId(new AccountIndex((ulong)entityEntry.Entity.Id)),
            AccountAddress.From(AccountAddressHelper.GetUniqueAddress()),
            CcdAmount.Zero,
            CcdAmount.Zero,
            CcdAmount.Zero,
            new BakerPoolInfo(
                afterRates,
                BakerPoolOpenStatus.OpenForAll,
                ""
                ),
            afterStatus,
            CcdAmount.Zero,
            null
            );
        var bakerPoolStatuses = () => Task.FromResult(new[] { bakerPoolStatus });
        
        // Act
        await target.UpdateCurrentPaydayStatusOnAllBakers(bakerPoolStatuses);

        // Assert
        var baker = await graphQlDbContext.Bakers.SingleAsync();
        baker.ActiveState.Pool.PaydayStatus.CommissionRates.BakingCommission.Should().Be(afterRates.BakingCommission.AsDecimal());
        baker.ActiveState.Pool.PaydayStatus.CommissionRates.FinalizationCommission.Should().Be(afterRates.FinalizationCommission.AsDecimal());
        baker.ActiveState.Pool.PaydayStatus.CommissionRates.TransactionCommission.Should().Be(afterRates.TransactionCommission.AsDecimal());
    }
    
    [Theory]
    [InlineData(0.42, 0.42)]
    [InlineData(0.52, 0.5)]
    public async Task WhenGetChainParameterUpdate_ThenOnlyAffectValidatorCommissionWhenBelow(decimal newMaxLimit, decimal expected)
    {
        // Arrange
        TruncateTables();
        var factoryMock = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_dbFixture.CreateGraphQlDbContext()));
        var target = new BakerImportHandler(factoryMock.Object, new NullMetrics());
        const decimal currentCommissions = 0.5m;
        var defaultBakerPool = BakerPool.CreateDefaultBakerPool();
        defaultBakerPool.CommissionRates.BakingCommission = currentCommissions;
        var activeBakerState = new ActiveBakerStateBuilder()
            .WithPool(defaultBakerPool)
            .Build();
        var build = new BakerBuilder()
            .WithState(activeBakerState)
            .Build();
        var graphQlDbContext = _dbFixture.CreateGraphQlDbContext();
        await graphQlDbContext.Bakers.AddAsync(build);
        await graphQlDbContext.SaveChangesAsync();
        graphQlDbContext.ChangeTracker.Clear();
        
        // Act
        await target.MaybeApplyCommissionRangeChanges(new ChainParametersChangedState
        (
            Previous: new ChainParametersV2
            {
                BakingCommissionRange = new CommissionRange { Min = 0, Max = currentCommissions },
                FinalizationCommissionRange = new CommissionRange(),
                TransactionCommissionRange = new CommissionRange()
            },
            Current: new ChainParametersV2
            {
                BakingCommissionRange = new CommissionRange { Min = 0, Max = newMaxLimit },
                FinalizationCommissionRange = new CommissionRange(),
                TransactionCommissionRange = new CommissionRange()
            }
        ));

        // Assert
        var singleAsync = await graphQlDbContext.Bakers.SingleAsync();
        singleAsync.ActiveState.Pool!.CommissionRates.BakingCommission.Should().Be(expected);
    }

    [Fact]
    public async Task TestFirstBlockAfterPaydayBakerAddition()
    {
        TruncateTables();
        var factoryMock = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_dbFixture.CreateGraphQlDbContext()));
        var target = new BakerImportHandler(factoryMock.Object, new NullMetrics());
        var address = AccountAddress.From("3rViPc7mHzabc586rt6HJ2bgSc3CJxAtnjh759hiefpVQoVTUs");
        const ProtocolVersion protocolVersion = ProtocolVersion.P4;
        var bakerId = new BakerId(new AccountIndex(1));
        var mintRate = MintRateExtensions.From(10);

        var bakerConfigured = new BakerConfigured(new List<IBakerEvent>{new BakerAddedEvent(
            new BakerKeysEvent(
                bakerId,
                address,
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                Array.Empty<byte>()
            ),
            CcdAmount.Zero,
            true
        )});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerConfigured)
            .WithSender(address)
            .WithCost(CcdAmount.Zero)
            .Build();
        
        var blockInfo = new BlockInfoBuilder()
            .WithProtocolVersion(protocolVersion)
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .WithTransactionHash(TransactionHash.From("d71b02cf129cf5f308131823945bdef23474edaea669acb08667e194d4b713ab"))
            .WithEnergyAmount(new EnergyAmount(0))
            .WithIndex(0)
            .Build();

        var rewardOverviewV1 = new RewardOverviewV1Builder()
            .WithProtocolVersion(protocolVersion)
            .WithNextPaydayTime(DateTimeOffset.Now)
            .WithNextPaydayMintRate(mintRate)
            .Build();

        var allBakerStatusesFunc = () => Task.FromResult(new[]
        {
            new BakerPoolStatus(
                bakerId,
                address,
                CcdAmount.Zero,
                CcdAmount.Zero,
                CcdAmount.Zero,
                new BakerPoolInfo(
                    new CommissionRates(AmountFraction.From(0), AmountFraction.From(0), AmountFraction.From(0)),
                    BakerPoolOpenStatus.OpenForAll,
                    ""),
                null,
                CcdAmount.Zero,
                null
            )
        });

        var passiveDelegationPoolStatusFunc = () => Task.FromResult(
            new PassiveDelegationStatus(
                CcdAmount.Zero,
                new CommissionRates(AmountFraction.From(0), AmountFraction.From(0), AmountFraction.From(0)),
                CcdAmount.Zero, 
                CcdAmount.Zero, 
                CcdAmount.Zero
        ));

        var blockDataPayload = new BlockDataPayloadBuilder()
            .WithBlockItemSummaries(new List<BlockItemSummary>{blockItemSummary})
            .WithBlockInfo(blockInfo)
            .WithRewardStatus(rewardOverviewV1)
            .WithAllBakerStatusesFunc(allBakerStatusesFunc)
            .WithPassiveDelegationPoolStatusFunc(passiveDelegationPoolStatusFunc)
            .Build();
        
        var result = await target.HandleBakerUpdates(
            blockDataPayload,
            new RewardsSummary(Array.Empty<AccountRewardSummary>()),
            new ChainParametersState(new ChainParametersV1Builder().Build()),
            new FirstBlockAfterPayday(DateTimeOffset.Now, 900),
        new ImportStateBuilder().Build()
        );

        var dbContext = _dbFixture.CreateGraphQlDbContext();
        var bakers = dbContext.Bakers.AsList();
        bakers.Count.Should().Be(1);
    }
    
    private static void TruncateTables()
    {
        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_bakers");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
        connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
    }
}
