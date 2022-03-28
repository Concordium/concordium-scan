using FluentAssertions;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Api.GraphQL.Accounts;

public class AccountTest : IAsyncLifetime
{
    private readonly GraphQlTestHelper _testHelper = new();

    public async Task InitializeAsync()
    {
        await _testHelper.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _testHelper.DisposeAsync();
    }

    /// <summary>
    /// The GraphQL built-in cursor based paging is actually just an offset based paging - this means that when new
    /// account statement entries are added, the pages will "shift". A custom cursor based paging has been implemented to mitigate this. 
    /// </summary>
    [Fact]
    public async Task GetAccountStatement_EnsurePagingCursorAreStableWhenNewEntriesAreAdded()
    {
        
        // Create initial data...
        _testHelper.DbContext.Accounts.Add(new AccountBuilder()
            .WithId(42)
            .WithCanonicalAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", true)
            .Build());
        
        _testHelper.DbContext.AccountStatementEntries.AddRange(
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(1).Build(),
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(2).Build(),
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(3).Build(),
            new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(4).Build());
        await _testHelper.DbContext.SaveChangesAsync();

        // ... request the "first" two of them...
        var result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                accountByAddress(accountAddress:""3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"") {
                    accountStatement(first: 2) {
                        nodes { 
                            amount 
                        }
                        pageInfo {
                            startCursor
                            endCursor
                        }
                    }
                }
            }");
        
        // ... and check that we get the right nodes...
        var root = result["accountByAddress"]?["accountStatement"];
        var actual = root?["nodes"]?.AsArray().Select(x => (long)x!["amount"]!).ToArray();
        actual.Should().Equal(4, 3);

        // ... remember the end cursor of this query...
        var endCursor = (string)root?["pageInfo"]?["endCursor"]!;
        
        // ... and add a new entry to the top of the chain (in the database)...
        _testHelper.DbContext.AccountStatementEntries.Add(new AccountStatementEntryBuilder().WithAccountId(42).WithAmount(5).Build());
        await _testHelper.DbContext.SaveChangesAsync();
        
        // ... and make a new request using the cursor from the previous query...
        result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                accountByAddress(accountAddress:""3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"") {
                    accountStatement(first: 2, after:""" + endCursor + @""") {
                        nodes { 
                            amount 
                        }
                        pageInfo {
                            startCursor
                            endCursor
                        }
                    }
                }
            }");

        // ... and ensure that we get the right blocks (that the inserted block hasn't shifted the paging cursor)
        root = result["accountByAddress"]?["accountStatement"];
        actual = root?["nodes"]?.AsArray().Select(x => (long)x!["amount"]!).ToArray();
        actual.Should().Equal(2, 1);
    }
}