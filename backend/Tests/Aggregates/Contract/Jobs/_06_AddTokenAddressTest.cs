using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tests.TestUtilities;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class _06_AddTokenAddressTest
{
    private readonly DatabaseFixture _fixture;
    
    public _06_AddTokenAddressTest(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenRunJob_ThenEnrichWithTokenAddress()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_tokens");
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            var first = new Token
            {
                ContractIndex = 1,
                ContractSubIndex = 0,
                TokenId = "01"
            };
            var second = new Token
            {
                ContractIndex = 2,
                ContractSubIndex = 0,
                TokenId = "02"
            };
            await context.AddRangeAsync(first, second);
        }

        var options = Options.Create(new ContractAggregateOptions());
        var job = new _06_AddTokenAddress(
            _fixture.CreateDbContractFactoryMock().Object,
            options
        );
        var parallelBatchJob = new ParallelBatchJob<_06_AddTokenAddress>(job, options);
        
        // Act
        await parallelBatchJob.StartImport(CancellationToken.None);
        
        // Assert
        parallelBatchJob.ShouldNodeImportAwait().Should().BeTrue();
        await using var assetContext = _fixture.CreateGraphQlDbContext();
        var tokenFirst = await assetContext.Tokens
            .SingleAsync(t => t.ContractIndex == 1 && t.ContractSubIndex == 0 && t.TokenId == "01");
        var tokenSecond = await assetContext.Tokens
            .SingleAsync(t => t.ContractIndex == 2 && t.ContractSubIndex == 0 && t.TokenId == "02");
        tokenFirst.TokenAddress!.Should().Be("LSYWgnCBmz");
        tokenSecond.TokenAddress!.Should().Be("LUjzdxXnte");
    }
}
