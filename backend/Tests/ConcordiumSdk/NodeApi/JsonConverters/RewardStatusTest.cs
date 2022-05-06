using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using FluentAssertions;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class RewardStatusTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public RewardStatusTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void Deserialize_V0_NoProtocolVersion()
    {
        var json = @"{
                ""totalAmount"": ""10000002372478948"",
                ""totalEncryptedAmount"": ""10020"",
                ""bakingRewardAccount"": ""2016607105"",
                ""finalizationRewardAccount"": ""4"",
                ""gasAccount"": ""10""
            }";

        var result = JsonSerializer.Deserialize<RewardStatusBase>(json, _serializerOptions)!;
        var typed = result.Should().BeOfType<RewardStatusV0>().Subject!;
        typed.TotalAmount.Should().Be(CcdAmount.FromMicroCcd(10000002372478948));
        typed.TotalEncryptedAmount.Should().Be(CcdAmount.FromMicroCcd(10020));
        typed.BakingRewardAccount.Should().Be(CcdAmount.FromMicroCcd(2016607105));
        typed.FinalizationRewardAccount.Should().Be(CcdAmount.FromMicroCcd(4));
        typed.GasAccount.Should().Be(CcdAmount.FromMicroCcd(10));
    }
    
    [Fact]
    public void Deserialize_V1()
    {
        var json = 
            @"{
                ""totalStakedCapital"": ""15000000000000"",
                ""finalizationRewardAccount"": ""446547400847"",
                ""protocolVersion"": 4,
                ""nextPaydayMintRate"": 2.611578781e-4,
                ""totalEncryptedAmount"": ""1234"",
                ""foundationTransactionRewards"": ""3344"",
                ""nextPaydayTime"": ""2022-05-05T11:59:43Z"",
                ""totalAmount"": ""219988491336196"",
                ""gasAccount"": ""42"",
                ""bakingRewardAccount"": ""893094801707""
            }";
        
        var result = JsonSerializer.Deserialize<RewardStatusBase>(json, _serializerOptions)!;
        var typed = result.Should().BeOfType<RewardStatusV1>().Subject!;
        typed.TotalAmount.Should().Be(CcdAmount.FromMicroCcd(219988491336196));
        typed.TotalEncryptedAmount.Should().Be(CcdAmount.FromMicroCcd(1234));
        typed.BakingRewardAccount.Should().Be(CcdAmount.FromMicroCcd(893094801707));
        typed.FinalizationRewardAccount.Should().Be(CcdAmount.FromMicroCcd(446547400847));
        typed.GasAccount.Should().Be(CcdAmount.FromMicroCcd(42));
        typed.FoundationTransactionRewards.Should().Be(CcdAmount.FromMicroCcd(3344));
        typed.NextPaydayTime.Should().Be(new DateTimeOffset(2022, 05, 05, 11, 59, 43, 0, TimeSpan.Zero));
        typed.NextPaydayMintRate.Should().Be(2.611578781e-4m);
        typed.TotalStakedCapital.Should().Be(CcdAmount.FromMicroCcd(15000000000000));
    }

}