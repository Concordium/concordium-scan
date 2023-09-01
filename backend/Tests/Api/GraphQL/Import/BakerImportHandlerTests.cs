using System.Collections.Generic;
using Application.Api.GraphQL.Extensions;
using Application.Api.GraphQL.Import;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using AccountAddress = Concordium.Sdk.Types.AccountAddress;
using AccountIndex = Concordium.Sdk.Types.AccountIndex;
using BakerId = Concordium.Sdk.Types.BakerId;
using BakerKeysEvent = Concordium.Sdk.Types.BakerKeysEvent;
using BakerPoolInfo = Concordium.Sdk.Types.BakerPoolInfo;
using BlockItemSummary = Concordium.Sdk.Types.BlockItemSummary;
using ChainParametersV1Builder = Tests.TestUtilities.Builders.GraphQL.ChainParametersV1Builder;
using CommissionRates = Concordium.Sdk.Types.CommissionRates;
using ProtocolVersion = Concordium.Sdk.Types.ProtocolVersion;
using TransactionHash = Concordium.Sdk.Types.TransactionHash;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class BakerImportHandlerTest
{
    private GraphQlDbContextFactoryStub _dbContextFactory;
    private BakerImportHandler _target;

    public BakerImportHandlerTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        _target = new BakerImportHandler(_dbContextFactory, new NullMetrics());

        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_bakers");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
        connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
    }

    [Fact]
    public async Task TestFirstBlockAfterPaydayBakerAddition()
    {
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
        
        var result = await _target.HandleBakerUpdates(
            blockDataPayload,
            new RewardsSummary(Array.Empty<AccountRewardSummary>()),
            new ChainParametersState(new ChainParametersV1Builder().Build()),
            new FirstBlockAfterPayday(DateTimeOffset.Now, 900),
        new ImportStateBuilder().Build()
        );

        var dbContext = _dbContextFactory.CreateDbContext();
        var bakers = dbContext.Bakers.AsList();
        bakers.Count.Should().Be(1);
    }
}