using System.Text;
using System.Text.Json;
using ConcordiumSdk.Utilities;
using FluentAssertions;

namespace Tests.ConcordiumSdk.Utilities;

public class Utf8JsonReaderExtensionsTest
{
    [Fact]
    public void ForwardReaderToTokenTypeAtDepth_EmptyJson()
    {
        var json = @"{}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(jsonBytes);
        reader.Read();

        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, 0);
        reader.BytesConsumed.Should().Be(jsonBytes.Length);
    }
    
    [Fact]
    public void ForwardReaderToTokenTypeAtDepth_CanFindEndObjectAtRequestedDepth()
    {
        var json = @"{
            ""prop1"": 10,
            ""prop2"": 20
        }";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(jsonBytes);
        reader.Read();
        
        reader.ForwardReaderToPropertyValue("prop1");
        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, 0);
        reader.BytesConsumed.Should().Be(jsonBytes.Length);
    }
    
    [Fact]
    public void ForwardReaderToTokenTypeAtDepth_CannotFindEndObjectAtRequestedDepth()
    {
        var json = @"{
            ""prop1"": 10,
            ""prop2"": 20
        }";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(jsonBytes);
        reader.Read();
        
        reader.ForwardReaderToPropertyValue("prop1");
        var thrown = false;
        try
        {
            reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, 1);
        }
        catch (Exception e)
        {
            thrown = true;
        }

        thrown.Should().BeTrue();
    }
}