using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore.Converters.Json;
using FluentAssertions;

namespace Tests.Aggregates.Contract.Configurations;

public sealed class CisEventConverterTest
{
    private readonly CisEventConverter _converter = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters =
        {
            new BigIntegerConverter(),
            new AddressConverter(),
            new ContractAddressConverter(),
            new AccountAddressConverter()
        }
    };

    private const string TransferEvent =
        "{\"tag\":4,\"data\":{\"TokenId\":\"00000001\",\"TokenAmount\":42,\"FromAddress\":{\"tag\":1,\"data\":\"48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e\"},\"ToAddress\":{\"tag\":2,\"data\":\"42,0\"},\"ContractIndex\":1423,\"ContractSubIndex\":0,\"TransactionHash\":\"foobar\",\"Parsed\":null}}";
    
    [Fact]
    public void WhenReadTransferEvent_ThenReturnParsedObject()
    {
        WhenReadEven_ThenReturnParsedObject(TransferEvent, typeof(CisTransferEvent));
    }
    
    [Fact]
    public void WhenWriteTransferEvent_ThenObjectWritten()
    {
        // Arrange
        var cisTransferEvent = new CisTransferEvent(
            
            contractIndex: 1423,
            contractSubIndex: 0,
            tokenId: "00000001",
            transactionHash: "foobar",
            parsed: null,
            tokenAmount: new BigInteger(42),
            toAddress: new ContractAddress(42,0),
            fromAddress:new AccountAddress("48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e")
        );

        WhenWriteEvent_ThenObjectWritten(cisTransferEvent, TransferEvent);
    }

    private const string TokenMetadataEvent = "{\"tag\":2,\"data\":{\"TokenId\":\"00000001\",\"MetadataUrl\":\"https://ipfs.io/ipfs/QmV5REE3HJRLTHdmqG18Wc5PBF3Nc9W5dQL4Rp7MxBsx8q?filename=nft.jpg\",\"HashHex\":null,\"ContractIndex\":1423,\"ContractSubIndex\":0,\"TransactionHash\":\"foobar\",\"Parsed\":null}}";
    
    [Fact]
    public void WhenReadTokenMetadataEvent_ThenReturnParsedObject()
    {
        WhenReadEven_ThenReturnParsedObject(BurnEvent, typeof(CisBurnEvent));
    }
    
    [Fact]
    public void WhenWriteTokenMetadataEvent_ThenObjectWritten()
    {
        // Arrange
        var cisTokenMetadataEvent = new CisTokenMetadataEvent(
            
            contractIndex: 1423,
            contractSubIndex: 0,
            tokenId: "00000001",
            transactionHash: "foobar",
            parsed: null,
            metadataUrl:"https://ipfs.io/ipfs/QmV5REE3HJRLTHdmqG18Wc5PBF3Nc9W5dQL4Rp7MxBsx8q?filename=nft.jpg",
            hashHex: null
        );

        WhenWriteEvent_ThenObjectWritten(cisTokenMetadataEvent, TokenMetadataEvent);
    }

    private const string BurnEvent =
        "{\"tag\":1,\"data\":{\"TokenId\":\"00000001\",\"TokenAmount\":1,\"FromAddress\":{\"tag\":1,\"data\":\"48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e\"},\"ContractIndex\":1423,\"ContractSubIndex\":0,\"TransactionHash\":\"foobar\",\"Parsed\":null}}";

    [Fact]
    public void WhenReadBurnEvent_ThenReturnParsedObject()
    {
        WhenReadEven_ThenReturnParsedObject(TokenMetadataEvent, typeof(CisTokenMetadataEvent));
    }
    
    [Fact]
    public void WhenWriteBurnEvent_ThenObjectWritten()
    {
        // Arrange
        var cisBurnEvent = new CisBurnEvent(
            fromAddress: new AccountAddress("48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e"),
            tokenAmount: new BigInteger(1),
            contractIndex: 1423,
            contractSubIndex: 0,
            tokenId: "00000001",
            transactionHash: "foobar",
            parsed: null
        );

        WhenWriteEvent_ThenObjectWritten(cisBurnEvent, BurnEvent);
    }
    
    private const string MintEvent =
        "{\"tag\":3,\"data\":{\"TokenId\":\"00000001\",\"TokenAmount\":1,\"ToAddress\":{\"tag\":1,\"data\":\"48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e\"},\"ContractIndex\":1423,\"ContractSubIndex\":0,\"TransactionHash\":\"foobar\",\"Parsed\":null}}";
    
    [Fact]
    public void WhenReadMintEvent_ThenReturnParsedObject()
    {
        WhenReadEven_ThenReturnParsedObject(MintEvent, typeof(CisMintEvent));
    }
    
    [Fact]
    public void WhenWriteMintEvent_ThenObjectWritten()
    {
        // Arrange
        var cisMintEvent = new CisMintEvent(
            tokenAmount: new BigInteger(1),
            toAddress: new AccountAddress("48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e"),
            contractIndex: 1423,
            contractSubIndex: 0,
            tokenId: "00000001",
            transactionHash: "foobar",
            parsed: null
        );
        
        WhenWriteEvent_ThenObjectWritten(cisMintEvent, MintEvent);
    }
    
    private void WhenReadEven_ThenReturnParsedObject(string cisEvent, Type expectedType)
    {
        // Arrange
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(cisEvent));
        reader.Read();
        
        // Act
        var actual = _converter.Read(ref reader, typeof(CisEvent), _jsonSerializerOptions);
        
        // Assert
        actual.Should().BeOfType(expectedType);
    }

    private void WhenWriteEvent_ThenObjectWritten(CisEvent cisEvent, string expected)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        // Act
        _converter.Write(writer, cisEvent, _jsonSerializerOptions);
        writer.Flush();
        
        // Assert
        var actual = Encoding.UTF8.GetString(stream.ToArray());
        actual.Should().Be(expected);
    }    
}
