using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public TokenTest(DatabaseFixture dbFixture)
    {
        _databaseFixture = dbFixture;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = { new CisEventDataConverter(), new AddressConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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
    public async Task GivenTokenWithEvent_WhenQueryToken_ThenTokenWithEvents()
    {
        // Arrange
        const ulong contractIndex = 1UL;
        const ulong contractSubindex = 0UL;
        const string tokenId = "42";
        const string amount = "42";
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
            await context.SaveChangesAsync();
        }
        var query = GetTokenQuery(contractIndex, tokenId);

        // Act
        var result = await _testHelper.ExecuteGraphQlQueryAsync(query);

        // Assert
        var tokenFromQuery = result["token"].Deserialize<Token>(_jsonSerializerOptions);
        result["token"]!["tokenEvents"]![0]!["id"] = 0; // Stub the base64 encoded id
        var tokenEventsFromQuery = result["token"]!["tokenEvents"].Deserialize<List<TokenEvent>>(_jsonSerializerOptions);

        tokenFromQuery.Should().NotBeNull();
        tokenFromQuery!.TokenId.Should().Be(tokenId);
        tokenFromQuery!.ContractIndex.Should().Be(contractIndex);
        tokenFromQuery!.ContractSubIndex.Should().Be(contractSubindex);
        
        tokenEventsFromQuery.Should().NotBeNull();
        tokenEventsFromQuery!.Count.Should().Be(1);
        var tokenEventFromQuery = tokenEventsFromQuery[0];
        tokenEventFromQuery.TokenId.Should().Be(tokenId);
        tokenEventFromQuery.ContractIndex.Should().Be(contractIndex);
        tokenEventFromQuery.ContractSubIndex.Should().Be(contractSubindex);
        tokenEventFromQuery.Event.Should().BeOfType<CisEventDataMint>();
        var eventDataMint = (tokenEventFromQuery.Event as CisEventDataMint)!;
        eventDataMint.Amount.Should().Be(amount);
        eventDataMint.To.Should().BeOfType<ContractAddress>();
        var to = (eventDataMint.To as ContractAddress)!;
        to.Index.Should().Be(contractIndex);
        to.SubIndex.Should().Be(contractSubindex);
    }

    private class AddressConverter : JsonConverter<Address>
    {
        public override Address? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (!root.TryGetProperty("__typename", out var typename))
                throw new JsonException("__typename property not present");

            var eventType = typename.GetString();
            switch (eventType)
            {
                case "ContractAddress":
                    var contractAddress = root.Deserialize<ContractAddress>(options);
                    return contractAddress;
                default:
                    throw new JsonException($"Unknown event type {eventType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private class CisEventDataConverter : JsonConverter<CisEventData>
    {
        public override CisEventData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (!root.TryGetProperty("__typename", out var typename))
                throw new JsonException("__typename property not present");
            
            var eventType = typename.GetString();
            switch (eventType)
            {
                case "CisEventDataMint":
                    var cisEventDataMint = root.Deserialize<CisEventDataMint>(options);
                    return cisEventDataMint;
                default:
                    throw new JsonException($"Unknown event type {eventType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, CisEventData value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
    
    private static string GetTokenQuery(ulong contractIndex, string tokenId)
    {
        return $@"
query {{
    token(contractIndex: {contractIndex}, contractSubIndex: 0, tokenId: ""{tokenId}"") {{
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
            }}
        }}      
    }}
}}
";
    }
}
