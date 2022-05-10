using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class AccountDelegationPendingChangeConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public AccountDelegationPendingChangeConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void RoundTrip_AccountDelegationRemovePendingV1()
    {
        var json = @"{
                         ""change"": ""RemoveStake"",
                         ""effectiveTime"": ""2022-04-29T11:32:15.75+00:00""
                     }";
        
        var result = JsonSerializer.Deserialize<AccountDelegationPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountDelegationRemovePending>(result);
        Assert.Equal(new DateTimeOffset(2022, 04, 29, 11, 32, 15, 750, TimeSpan.Zero), typed.EffectiveTime);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AccountDelegationReduceStakePending()
    {
        var json = @"{
                         ""change"": ""ReduceStake"",
                         ""newStake"": ""1000010000"",
                         ""effectiveTime"": ""2022-04-29T10:05:25.5+00:00""
                     }";
        
        var result = JsonSerializer.Deserialize<AccountDelegationPendingChange>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<AccountDelegationReduceStakePending>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(1000010000), typed.NewStake);
        Assert.Equal(new DateTimeOffset(2022, 04, 29, 10, 05, 25, 500, TimeSpan.Zero), typed.EffectiveTime);

        var serialized = JsonSerializer.Serialize(result, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}