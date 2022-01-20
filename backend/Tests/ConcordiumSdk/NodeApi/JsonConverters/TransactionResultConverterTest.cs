using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.NodeApi.Types.JsonConverters;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TransactionResultConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionResultConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void Deserialize_Success()
    {
        var json = "{\"events\": [{\"tag\": \"AccountCreated\", \"contents\": \"3aTTghVWSQPRKEXhE5a4aUWsvSeNEMHYFa25sgxAP3HZVeU25p\"}], \"outcome\": \"success\"}";
        var result = JsonSerializer.Deserialize<TransactionResult>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionSuccessResult>(result);
        Assert.Single(typed.Events);
    }
    
    [Fact]
    public void Deserialize_Reject()
    {
        var json = "{\"outcome\": \"reject\", \"rejectReason\": {\"tag\": \"OutOfEnergy\"}}";
        var result = JsonSerializer.Deserialize<TransactionResult>(json, _serializerOptions);
        var rejectResult = Assert.IsType<TransactionRejectResult>(result);
        Assert.IsType<OutOfEnergy>(rejectResult.Reason);
    }
}