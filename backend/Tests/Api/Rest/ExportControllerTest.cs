using Application.Api.Rest;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using Application.Api.GraphQL.Configurations;
using Application.Api.GraphQL.EfCore;
using Application.Database;
using Dapper;
using FluentAssertions;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Api.Rest;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class ExportControllerTest : IAsyncLifetime
{

    private readonly RestTestHelper _testHelper = new();
    private readonly DatabaseSettings _dbSettings;
    public ExportControllerTest(DatabaseFixture dbFixture)
    {
        _dbSettings = dbFixture.DatabaseSettings;
    }

    public async Task InitializeAsync()
    {
        await _testHelper.InitializeAsync(_dbSettings);
    }

    public async Task DisposeAsync()
    {
        await _testHelper.DisposeAsync();
    }

    [Fact]
    public async void accountStatementWith33DaySpanIsNotAllowed()
    {
        // Arrange
        var startDate = DateTime.SpecifyKind(new DateTime(2020, 11, 1), DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(new DateTime(2020, 12, 4), DateTimeKind.Utc);

        var controller = new ExportController(_testHelper.dbContextFactory);
        var address = "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P";

        _testHelper.DbContext.Accounts.Add(new AccountBuilder()
            .WithId(42)
            .WithCanonicalAddress(address, true)
            .Build());
        await _testHelper.DbContext.SaveChangesAsync();

        // Act
        var result = await controller.GetStatementExport(address, startDate, endDate);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<string>(badRequestResult.Value).Should().Contain("time span exceeds");
    }

    [Fact]
    public async void transactionOutsideSpecifiedTimeStampsAreNotReturned()
    {
        // Arrange
        var date1 = new DateTime(2020, 12, 1);
        var date2 = new DateTime(2020, 12, 7);
        var date3 = new DateTime(2020, 12, 12);
        var startDate = DateTime.SpecifyKind(new DateTime(2020, 12, 5), DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(new DateTime(2020, 12, 10), DateTimeKind.Utc);

        var controller = new ExportController(_testHelper.dbContextFactory);
        var address = "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P";

        _testHelper.DbContext.Accounts.Add(new AccountBuilder()
            .WithId(42)
            .WithCanonicalAddress(address, true)
            .Build());

        _testHelper.DbContext.AccountStatementEntries.AddRange(
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(1).WithTimestamp(date1).Build(),
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(2).WithTimestamp(date2).Build(),
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(2).WithTimestamp(date3).Build()
        );
        await _testHelper.DbContext.SaveChangesAsync();

        // Act
        var actionResult = await controller.GetStatementExport(address, startDate, endDate);
        var result = Assert.IsType<FileContentResult>(actionResult);
        string csv = System.Text.Encoding.UTF8.GetString(result.FileContents);

        // Assert
        Regex.Matches(csv, "\n").Count.Should().Be(2);
    }

}
