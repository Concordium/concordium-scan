using System.Text.Json;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.NodeApi.Types.JsonConverters;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TransactionResultConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionResultConverterTest()
    {
        _serializerOptions = new JsonSerializerOptions( );
        _serializerOptions.Converters.Add(new TransactionResultConverter());
    }

    [Fact]
    public void Deserialize_Success()
    {
        var json = "{\"events\": [{\"foo\": \"bar\"}, {\"lorem\": 1}], \"outcome\": \"success\"}";
        var result = JsonSerializer.Deserialize<TransactionResult>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionSuccessResult>(result);
        Assert.Equal(2, typed.Events.EnumerateArray().Count());
    }
    
    [Fact]
    public void Deserialize_Reject()
    {
        var json = "{\"outcome\": \"reject\", \"rejectReason\": {\"tag\": \"AmountTooLarge\"}}";
        var result = JsonSerializer.Deserialize<TransactionResult>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionRejectResult>(result);
        Assert.Equal("AmountTooLarge", typed.Tag);
    }
}