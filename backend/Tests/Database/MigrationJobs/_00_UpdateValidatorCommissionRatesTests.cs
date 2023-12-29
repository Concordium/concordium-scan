using System.Threading;
using Application.Api.GraphQL.Bakers;
using Application.Database.MigrationJobs;
using Application.Import.ConcordiumNode;
using Concordium.Sdk.Types;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using BakerPoolOpenStatus = Concordium.Sdk.Types.BakerPoolOpenStatus;
using CommissionRates = Concordium.Sdk.Types.CommissionRates;

namespace Tests.Database.MigrationJobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class _00_UpdateValidatorCommissionRatesTests
{
    private readonly DatabaseFixture _databaseFixture;

    public _00_UpdateValidatorCommissionRatesTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task WhenUpdateValidatorsCommissionRates_ThenUpdateToLatestFromChain()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_main_migration_jobs");
        await DatabaseFixture.TruncateTables("graphql_bakers");
        var dbFactory = _databaseFixture.CreateDbContractFactoryMock();

        var clientMock = new Mock<IConcordiumNodeClient>();
        const long firstBakerId = 1L;
        const decimal firstTransactionCommissionOld = 0.05m;
        const decimal firstBlockCommissionOld = 0.15m;
        const decimal firstTransactionCommissionNew = 0.2m;
        const decimal firstBlockCommissionNew = 0.3m;
        const long secondBakerId = 2L;
        const decimal secondTransactionCommission = 0.3m;
        const decimal secondBlockCommission = 0.4m;

        // Create validators and store to database
        var firstBaker = BuildBaker(firstBakerId, firstTransactionCommissionOld, firstBlockCommissionOld);
        var secondBaker = BuildBaker(secondBakerId, secondTransactionCommission, secondBlockCommission);
        await _databaseFixture.AddAsync(firstBaker, secondBaker);

        // Create validator data fetched from node.
        // Only first validator has commission rates different from those already in database.
        var firstBakerPoolStatus = CreateBakerPoolStatus(firstBakerId, firstTransactionCommissionNew, firstBlockCommissionNew);
        var secondBakerPoolStatus = CreateBakerPoolStatus(secondBakerId, secondTransactionCommission, secondBlockCommission);
        clientMock.Setup(c => c.GetPoolInfoAsync(
                It.IsAny<BakerId>(),
                It.IsAny<IBlockHashInput>(),
                It.IsAny<CancellationToken>()))
            .Returns<BakerId, IBlockHashInput, CancellationToken>((id, _, _) =>
                Task.FromResult(id.Id.Index == firstBakerId ? firstBakerPoolStatus : secondBakerPoolStatus));

        var job = new _00_UpdateValidatorCommissionRates(
            dbFactory.Object,
            clientMock.Object,
            Options.Create(new MainMigrationJobOptions())
        );

        // Act
        await job.StartImport(CancellationToken.None);

        // Assert
        await using var context = _databaseFixture.CreateGraphQlDbContext();
        
        var bakerOne = await context.Bakers.SingleAsync(b => b.Id == firstBakerId);
        bakerOne.ActiveState.Pool.CommissionRates.TransactionCommission.Should().Be(firstTransactionCommissionNew);
        bakerOne.ActiveState.Pool.CommissionRates.BakingCommission.Should().Be(firstBlockCommissionNew);
        
        var bakerSecond = await context.Bakers.SingleAsync(b => b.Id == secondBakerId);
        bakerSecond.ActiveState.Pool.CommissionRates.TransactionCommission.Should().Be(secondTransactionCommission);
        bakerSecond.ActiveState.Pool.CommissionRates.BakingCommission.Should().Be(secondBlockCommission);
    }
    
    private static BakerPoolStatus CreateBakerPoolStatus(
        ulong bakerId,
        decimal transactionCommission,
        decimal blockCommission
    )
    {
        return new BakerPoolStatus(
            new BakerId(new AccountIndex(bakerId)),
            AccountAddress.From(AccountAddressHelper.GetUniqueAddress()),
            CcdAmount.Zero,
            CcdAmount.Zero,
            CcdAmount.Zero,
            new BakerPoolInfo(
                new CommissionRates(AmountFraction.From(transactionCommission),
                    AmountFraction.From(1),
                    AmountFraction.From(blockCommission)),
                BakerPoolOpenStatus.OpenForAll, ""),
            null,
            CcdAmount.Zero, null
        );
    }

    private static Baker BuildBaker(
        long bakerId,
        decimal transactionCommission,
        decimal blockCommission
    )
    {
        var bakerPool = new BakerPoolBuilder()
            .WithCommissionRates(
                transactionCommission: transactionCommission,
                bakingCommission: blockCommission)
            .Build();
        var activeBakerState = new ActiveBakerStateBuilder()
            .WithPool(bakerPool)
            .Build();
        var build = new BakerBuilder()
            .WithState(activeBakerState)
            .WithId(bakerId)
            .Build();
        return build;
    }
}
