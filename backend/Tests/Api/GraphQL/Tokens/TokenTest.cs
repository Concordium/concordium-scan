using Application.Api.GraphQL;
using Application.Api.GraphQL.Tokens;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Api.GraphQL.Tokens;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class TokenTest : IAsyncLifetime
{
    private readonly GraphQlTestHelper _testHelper = new();
    private readonly DatabaseFixture _databaseFixture;

    public TokenTest(DatabaseFixture dbFixture)
    {
        _databaseFixture = dbFixture;
    }

    public async Task InitializeAsync()
    {
        await _testHelper.InitializeAsync(_databaseFixture.DatabaseSettings);
    }

    public async Task DisposeAsync()
    {
        await _testHelper.DisposeAsync();
    }

    [Fact]
    public async Task WhenQueryTokenEvents_ThenGetEvents()
    {
        // Arrange
        const ulong contractIndex = 1UL;
        const ulong contractSubindex = 0UL;
        const string tokenId = "42";
        var cisEventDataMint = new CisEventDataMint("42", new ContractAddress(contractIndex,contractSubindex));
        var tokenEvent = new TokenEvent(
            contractIndex, contractSubindex, tokenId, 1, cisEventDataMint
        );
        var token = new Token
        {
            ContractIndex = contractIndex,
            ContractSubIndex = contractSubindex,
            TokenId = tokenId
        };
        await DatabaseFixture.TruncateTables("graphql_tokens", "graphql_account_tokens", "graphql_token_events");
        await using (var context = _databaseFixture.CreateGraphQlDbContext())
        {
            context.TokenEvents.Add(tokenEvent);
            context.Tokens.Add(token);
        }
        
        // Act
        var result = await _testHelper.ExecuteGraphQlQueryAsync(GetQuery(1));

        // Assert
        result.Should().NotBeNull();
    }
    
    private static string GetQuery(int count)
    {
        return $@"
query {{
    tokens(first: {count}) {{
        nodes {{
            contractIndex
            contractSubIndex
            metadataUrl
            tokenId
            tokens {{
                accountId
                balance
                contractIndex
                contractSubIndex
                index
                tokenId
            }}
            tokenEvents {{
                id
                contractIndex
                contractSubIndex
                tokenId
                transactionId
                event {{
                    __typename
                    ... on CisEventDataBurn {{
                        amount
                        from {{
                            __typename
                            ... on AccountAddress {{
                                asString
                            }}
                            ... on ContractAddress {{
                                asString
                                index
                                subIndex
                            }}
                        }}
                    }}
                    ... on CisEventDataMetadataUpdate {{
                        metadataUrl
                        metadataHashHex
                    }}
                    ... on CisEventDataMint {{
                        amount
                        to {{
                            __typename
                            ... on AccountAddress {{
                                asString
                            }}
                            ... on ContractAddress {{
                                asString
                                index
                                subIndex
                            }}
                        }}
                    }}
                    ... on CisEventDataTransfer {{
                        amount
                        from {{
                            __typename
                            ... on AccountAddress {{
                                asString
                            }}
                            ... on ContractAddress {{
                                asString
                                index
                                subIndex
                            }}
                        }}                        
                        to {{
                            __typename
                            ... on AccountAddress {{
                                asString
                            }}
                            ... on ContractAddress {{
                                asString
                                index
                                subIndex
                            }}
                        }}
                    }}
                    ... on CisEventDataUpdateOperator {{
                        operator {{
                            __typename
                            ... on AccountAddress {{
                                asString
                            }}
                            ... on ContractAddress {{
                                asString
                                index
                                subIndex
                            }}
                        }}                        
                        owner {{
                            __typename
                            ... on AccountAddress {{
                                asString
                            }}
                            ... on ContractAddress {{
                                asString
                                index
                                subIndex
                            }}
                        }}
                        update
                    }}
                }}
            }}
        }}
    }}
}}
";
    }
}
