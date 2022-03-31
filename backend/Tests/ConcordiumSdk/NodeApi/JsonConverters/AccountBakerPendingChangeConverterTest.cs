using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class AccountBakerPendingChangeConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public AccountBakerPendingChangeConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void RoundTrip_RemoveBaker()
    {
        var json = "{\n\"change\":\"RemoveBaker\",\n\"epoch\":224\n}";
        var result = JsonSerializer.Deserialize<AccountBakerPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountBakerRemovePending>(result);
        Assert.Equal(224UL, typed.Epoch);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_ReduceStake()
    {
        var json = "{\"change\":\"ReduceStake\",\"newStake\":\"15466\",\"epoch\":224}";
        var result = JsonSerializer.Deserialize<AccountBakerPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountBakerReduceStakePending>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(15466), typed.NewStake);
        Assert.Equal(224UL, typed.Epoch);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}