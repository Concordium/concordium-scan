using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Application.Api.GraphQL.EfCore.Converters.Json;
using FluentAssertions;

namespace Tests.Api.GraphQL.EfCore.Converters.Json;

public sealed class BigIntegerConverterTest
{
    private readonly BigIntegerConverter _converter = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new();

    [Fact]
    public void WhenWrite_ThenObjectWritten()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var bigInteger = new BigInteger(42);
        
        // Act
        _converter.Write(writer, bigInteger, _jsonSerializerOptions);
        writer.Flush();
        
        // Assert
        var actual = Encoding.UTF8.GetString(stream.ToArray());
        actual.Should().Be("42");
    }

    [Fact]
    public void WhenRead_ThenReturnParsedObject()
    {
        // Arrange
        var expected = new BigInteger(42);
        var chars = expected.ToString(NumberFormatInfo.InvariantInfo);
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(chars));
        reader.Read();
        
        // Act
        var actual = _converter.Read(ref reader, typeof(BigInteger), _jsonSerializerOptions);
        
        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GivenWrongFormat_WhenRead_ThenThrowException()
    {
        // Arrange
        const string input = "\"42\"";
        var chars = input.ToString(NumberFormatInfo.InvariantInfo);
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(chars));
        reader.Read();
        
        try
        {
            // Act
            _converter.Read(ref reader, typeof(BigInteger), _jsonSerializerOptions);
            Assert.Fail("Should throw exception");
        }
        catch (Exception e)
        {
            // Assert
            e.Should().BeOfType<JsonException>();
        }
    }
}
