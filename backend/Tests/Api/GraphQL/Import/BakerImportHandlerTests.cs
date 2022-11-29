using Application.Api.GraphQL.Import;
using Application.Import;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

[Collection("Baker Import Handler")]
public class BakerImportHandlerTest : IClassFixture<DatabaseFixture>
{
    private GraphQlDbContextFactoryStub _dbContextFactory;
    private BakerImportHandler _target;

    public BakerImportHandlerTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new BakerImportHandler(_dbContextFactory, new NullMetrics());

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_bakers");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
        connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
    }

    [Fact]
    public async Task TestFirstBlockAfterPaydayBakerAddition()
    {
        var address = new AccountAddress("3rViPc7mHzabc586rt6HJ2bgSc3CJxAtnjh759hiefpVQoVTUs");
        var result = await _target.HandleBakerUpdates(new BlockDataPayload(
            new BlockInfo() {
            },
            new BlockSummaryV1()
            {
                ProtocolVersion = 4,
                TransactionSummaries = new TransactionSummary[] {
                    new TransactionSummary(
                        address,
                        new TransactionHash("d71b02cf129cf5f308131823945bdef23474edaea669acb08667e194d4b713ab"),
                        CcdAmount.Zero, 0,
                        TransactionType.Get(AccountTransactionType.ConfigureBaker),
                        new TransactionSuccessResult() {
                            Events = new TransactionResultEvent[] {
                                new BakerAdded(CcdAmount.Zero, true, 1, address, "", "", "")
                                }
                        },
                        0),
                }
            },
            new AccountInfosRetrieved(new AccountInfo[0], new AccountInfo[0]),
            new RewardStatusV1(
                new CcdAmount(),
                new CcdAmount(),
                new CcdAmount(),
                new CcdAmount(),
                new CcdAmount(),
                new CcdAmount(),
                DateTimeOffset.Now,
                10,
                new CcdAmount()
                ),
                () => Task<BakerPoolStatus[]>.FromResult(new BakerPoolStatus[1] {
                    new BakerPoolStatus(
                        1,
                        address,
                        CcdAmount.Zero,
                        CcdAmount.Zero,
                        CcdAmount.Zero,
                        new BakerPoolInfo(new CommissionRates(0, 0, 0), BakerPoolOpenStatus.OpenForAll, ""),null, CcdAmount.Zero
                        )
                }),
                () => Task.FromResult(new PoolStatusPassiveDelegation(
                    CcdAmount.Zero,
                    new CommissionRates(0, 0, 0),
                    CcdAmount.Zero,
                    CcdAmount.Zero,
                    CcdAmount.Zero
                ))
        ),
        new RewardsSummary(new AccountRewardSummary[0]),
        new ChainParametersState(new ChainParametersV1Builder().Build()),
        new FirstBlockAfterPayday(DateTimeOffset.Now, 900),
        new ImportStateBuilder().Build()
        );

        var dbContext = _dbContextFactory.CreateDbContext();
        var bakers = dbContext.Bakers.AsList();
        bakers.Count.Should().Be(1);
    }
}