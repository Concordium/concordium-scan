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
    public void RoundTrip_AccountBakerRemovePendingV0()
    {
        var json = "{\n\"change\":\"RemoveBaker\",\n\"epoch\":224\n}";
        var result = JsonSerializer.Deserialize<AccountBakerPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountBakerRemovePendingV0>(result);
        Assert.Equal(224UL, typed.Epoch);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AccountBakerRemovePendingV1()
    {
        var json = @"{
                         ""change"": ""RemoveStake"",
                         ""effectiveTime"": ""2022-04-29T11:32:15.75+00:00""
                     }";
        
         var result = JsonSerializer.Deserialize<AccountBakerPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountBakerRemovePendingV1>(result);
        Assert.Equal(new DateTimeOffset(2022, 04, 29, 11, 32, 15, 750, TimeSpan.Zero), typed.EffectiveTime);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AccountBakerReduceStakePendingV0()
    {
        var json = "{\"change\":\"ReduceStake\",\"newStake\":\"15466\",\"epoch\":224}";
        var result = JsonSerializer.Deserialize<AccountBakerPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountBakerReduceStakePendingV0>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(15466), typed.NewStake);
        Assert.Equal(224UL, typed.Epoch);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AccountBakerReduceStakePendingV1()
    {
        var json = @"{
                         ""change"": ""ReduceStake"",
                         ""newStake"": ""1000010000"",
                         ""effectiveTime"": ""2022-04-29T10:05:25.5+00:00""
                     }";
        
        var result = JsonSerializer.Deserialize<AccountBakerPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountBakerReduceStakePendingV1>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(1000010000), typed.NewStake);
        Assert.Equal(new DateTimeOffset(2022, 04, 29, 10, 05, 25, 500, TimeSpan.Zero), typed.EffectiveTime);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}