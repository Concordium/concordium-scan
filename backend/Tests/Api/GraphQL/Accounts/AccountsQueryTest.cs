using FluentAssertions;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Api.GraphQL.Accounts;

public class AccountsQueryTest : IAsyncLifetime
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
    /// accounts are added, the pages will "shift". A custom cursor based paging has been implemented to mitigate this. 
    /// </summary>
    [Fact]
    public async Task GetAccounts_EnsurePagingCursorAreStableWhenNewAccountsAreAdded()
    {
        // Create some initial accounts...
        await _testHelper.DbContext.Accounts.AddRangeAsync(
            new AccountBuilder().WithId(1).WithAddress("3wxwKbVMNj6ER4CnFteedrJxqSYgdnV6KEvvjXaBaCSYVDNfEL").Build(),
            new AccountBuilder().WithId(2).WithAddress("4QkDsFc22r86cUusXfCKTuT3Sf15vASAyBqyMyDiFQqeHxwUX4").Build(),
            new AccountBuilder().WithId(3).WithAddress("3xR6FKrXt35Vn3bBbQNVEFaTZinwWfEcFayhVWFpH4KkXBtVGP").Build(),
            new AccountBuilder().WithId(4).WithAddress("3ECDy9yqSppefrias43nm1rntu1tLYFbBL52u8xnPaKQgVwEb6").Build(),
            new AccountBuilder().WithId(5).WithAddress("3G47kjPEXKyDW82MuwUi6y6wcY96pRh8PPZn7zPUreK4QEpNkc").Build());
        await _testHelper.DbContext.SaveChangesAsync();

        // ... request the "first" two of them...
        var result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                accounts(first:2) {
                    nodes { 
                        address 
                    }
                    pageInfo {
                        startCursor
                        endCursor
                    }
                }
            }");
        
        // ... and check that we get the right nodes...
        var accountsNode = result["accounts"];
        var actual = accountsNode?["nodes"]?.AsArray().Select(x => (string)x!["address"]!).ToArray();
        actual.Should().Equal("3G47kjPEXKyDW82MuwUi6y6wcY96pRh8PPZn7zPUreK4QEpNkc", "3ECDy9yqSppefrias43nm1rntu1tLYFbBL52u8xnPaKQgVwEb6");

        // ... remember the end cursor of this query...
        var endCursor = (string)accountsNode?["pageInfo"]?["endCursor"]!;

        // ... and add a new block to the top of the chain (in the database)...
        await _testHelper.DbContext.Accounts.AddRangeAsync(new AccountBuilder().WithId(6).WithAddress("33jdgehzu3RFEMwLRRxyMtPxQgsGX3xzPagEx6L32Jsdphw7ys").Build());
        await _testHelper.DbContext.SaveChangesAsync();
        
        // ... and make a new request using the cursor from the previous query...
        result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                accounts(first:2, after:""" + endCursor + @""") {
                    nodes { 
                        address 
                    }
                    pageInfo {
                        startCursor
                        endCursor
                    }
                }
            }");

        // ... and ensure that we get the right blocks (that the inserted block hasn't shifted the paging cursor)
        accountsNode = result["accounts"];
        actual = accountsNode?["nodes"]?.AsArray().Select(x => (string)x!["address"]!).ToArray();
        actual.Should().Equal("3xR6FKrXt35Vn3bBbQNVEFaTZinwWfEcFayhVWFpH4KkXBtVGP", "4QkDsFc22r86cUusXfCKTuT3Sf15vASAyBqyMyDiFQqeHxwUX4");
    }
}