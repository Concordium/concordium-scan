using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using FluentAssertions;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class ChainParametersTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public ChainParametersTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void Deserialize_V0()
    {
        var json = @"{
            ""minimumThresholdForBaking"": ""14000000000"",
            ""rewardParameters"": {
                ""mintDistribution"": {
                    ""mintPerSlot"": 7.555665e-10,
                    ""bakingReward"": 0.85,
                    ""finalizationReward"": 5.0e-2
                },
                ""transactionFeeDistribution"": {
                    ""gasAccount"": 0.45,
                    ""baker"": 0.45
                },
                ""gASRewards"": {
                    ""chainUpdate"": 5.0e-3,
                    ""accountCreation"": 2.0e-2,
                    ""baker"": 0.25,
                    ""finalizationProof"": 5.0e-3
                }
            },
            ""microGTUPerEuro"": {
                ""denominator"": 549755813888,
                ""numerator"": 14437531196612023437
            },
            ""foundationAccountIndex"": 13,
            ""accountCreationLimit"": 10,
            ""bakerCooldownEpochs"": 166,
            ""electionDifficulty"": 2.5e-2,
            ""euroPerEnergy"": {
                ""denominator"": 50000,
                ""numerator"": 1
            }
        }";

        var result = JsonSerializer.Deserialize<ChainParametersV0>(json, _serializerOptions)!;
        result.ElectionDifficulty.Should().Be(0.025m);
        result.EuroPerEnergy.Denominator.Should().Be(50000);
        result.EuroPerEnergy.Numerator.Should().Be(1);
        result.MicroGTUPerEuro.Denominator.Should().Be(549755813888);
        result.MicroGTUPerEuro.Numerator.Should().Be(14437531196612023437);
        result.BakerCooldownEpochs.Should().Be(166);
        result.AccountCreationLimit.Should().Be(10);
        result.RewardParameters.MintDistribution.MintPerSlot.Should().Be(7.555665e-10m);
        result.RewardParameters.MintDistribution.BakingReward.Should().Be(0.85m);
        result.RewardParameters.MintDistribution.FinalizationReward.Should().Be(5.0e-2m);
        result.RewardParameters.TransactionFeeDistribution.Baker.Should().Be(0.45m);
        result.RewardParameters.TransactionFeeDistribution.GasAccount.Should().Be(0.45m);
        result.RewardParameters.GASRewards.Baker.Should().Be(0.25m);
        result.RewardParameters.GASRewards.FinalizationProof.Should().Be(5.0e-3m);
        result.RewardParameters.GASRewards.AccountCreation.Should().Be(2.0e-2m);
        result.RewardParameters.GASRewards.ChainUpdate.Should().Be(5.0e-3m);
        result.FoundationAccountIndex.Should().Be(13);
        result.MinimumThresholdForBaking.Should().Be(CcdAmount.FromMicroCcd(14000000000));
    }
    
    [Fact]
    public void Deserialize_V1()
    {
        var json = @"{
           ""mintPerPayday"": 1.088e-5,
           ""rewardParameters"": {
              ""mintDistribution"": {
                 ""bakingReward"": 0.6,
                 ""finalizationReward"": 0.3
              },
              ""transactionFeeDistribution"": {
                 ""gasAccount"": 0.45,
                 ""baker"": 0.45
              },
              ""gASRewards"": {
                 ""chainUpdate"": 5.0e-3,
                 ""accountCreation"": 2.0e-3,
                 ""baker"": 0.25,
                 ""finalizationProof"": 5.0e-3
              }
           },
           ""poolOwnerCooldown"": 10800,
           ""capitalBound"": 0.25,
           ""microGTUPerEuro"": {
              ""denominator"": 447214545287,
              ""numerator"": 8570751029767503872
           },
           ""rewardPeriodLength"": 4,
           ""passiveTransactionCommission"": 0.1,
           ""leverageBound"": {
              ""denominator"": 1,
              ""numerator"": 3
           },
           ""foundationAccountIndex"": 5,
           ""passiveFinalizationCommission"": 1.0,
           ""delegatorCooldown"": 7200,
           ""bakingCommissionRange"": {
              ""max"": 5.0e-2,
              ""min"": 5.0e-2
           },
           ""passiveBakingCommission"": 0.1,
           ""accountCreationLimit"": 10,
           ""finalizationCommissionRange"": {
              ""max"": 1.0,
              ""min"": 1.0
           },
           ""electionDifficulty"": 2.5e-2,
           ""euroPerEnergy"": {
              ""denominator"": 1000000,
              ""numerator"": 1
           },
           ""transactionCommissionRange"": {
              ""max"": 5.0e-2,
              ""min"": 5.0e-2
           },
           ""minimumEquityCapital"": ""14000""
        }";
       
        var result = JsonSerializer.Deserialize<ChainParametersV1>(json, _serializerOptions)!;
       
        result.ElectionDifficulty.Should().Be(0.025m);
        result.EuroPerEnergy.Denominator.Should().Be(1000000);
        result.EuroPerEnergy.Numerator.Should().Be(1);
        result.MicroGTUPerEuro.Denominator.Should().Be(447214545287);
        result.MicroGTUPerEuro.Numerator.Should().Be(8570751029767503872);
        result.PoolOwnerCooldown.Should().Be(10800);
        result.DelegatorCooldown.Should().Be(7200);
        result.RewardPeriodLength.Should().Be(4);
        result.MintPerPayday.Should().Be(0.00001088m);
        result.AccountCreationLimit.Should().Be(10);
        result.RewardParameters.MintDistribution.BakingReward.Should().Be(0.6m);
        result.RewardParameters.MintDistribution.FinalizationReward.Should().Be(0.3m);
        result.RewardParameters.TransactionFeeDistribution.Baker.Should().Be(0.45m);
        result.RewardParameters.TransactionFeeDistribution.GasAccount.Should().Be(0.45m);
        result.RewardParameters.GASRewards.Baker.Should().Be(0.25m);
        result.RewardParameters.GASRewards.FinalizationProof.Should().Be(5.0e-3m);
        result.RewardParameters.GASRewards.AccountCreation.Should().Be(2.0e-3m);
        result.RewardParameters.GASRewards.ChainUpdate.Should().Be(5.0e-3m);
        result.FoundationAccountIndex.Should().Be(5);
        result.PassiveFinalizationCommission.Should().Be(1.0m);
        result.PassiveBakingCommission.Should().Be(0.1m);
        result.PassiveTransactionCommission.Should().Be(0.1m);
        result.FinalizationCommissionRange.Min.Should().Be(1.0m);
        result.FinalizationCommissionRange.Max.Should().Be(1.0m);
        result.BakingCommissionRange.Min.Should().Be(5.0e-2m);
        result.BakingCommissionRange.Max.Should().Be(5.0e-2m);
        result.TransactionCommissionRange.Min.Should().Be(5.0e-2m);
        result.TransactionCommissionRange.Max.Should().Be(5.0e-2m);
        result.MinimumEquityCapital.Should().Be(CcdAmount.FromMicroCcd(14000));
        result.CapitalBound.Should().Be(0.25m);
        result.LeverageBound.Denominator.Should().Be(1);
        result.LeverageBound.Numerator.Should().Be(3);
    }
}