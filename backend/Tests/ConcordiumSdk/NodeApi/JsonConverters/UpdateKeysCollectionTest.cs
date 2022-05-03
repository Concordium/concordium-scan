using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using FluentAssertions;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class UpdateKeysCollectionTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public UpdateKeysCollectionTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void Deserialize_V0()
    {
        var json = @"{
            ""rootKeys"": {
                ""keys"": [
                    {
                        ""verifyKey"": ""0e87edd19aea0fe6bf70aaf7c4b27b28710d3f80f3c459d0fd4d1b56b82768d0"",
                        ""schemeId"": ""Ed25519""
                    },
                    {
                        ""verifyKey"": ""583100946b67ae0536d86774b3a86d69d39f9421a49953c42dd8e465535e73b1"",
                        ""schemeId"": ""Ed25519""
                    }
                ],
                ""threshold"": 2
            },
            ""level2Keys"": {
                ""mintDistribution"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""addAnonymityRevoker"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""transactionFeeDistribution"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""bakerStakeThreshold"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""microGTUPerEuro"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""protocol"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""addIdentityProvider"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""paramGASRewards"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""emergency"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""keys"": [
                    {
                        ""verifyKey"": ""b709d87b3151890ca9635f81e321feff8a39560ad3aed71fa0272086f4e207b2"",
                        ""schemeId"": ""Ed25519""
                    },
                    {
                        ""verifyKey"": ""e3494948226d8bbac20d7e0ebdbfa6e680d56a92c28e3fef5d929b61feba90db"",
                        ""schemeId"": ""Ed25519""
                    }
                ],
                ""foundationAccount"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""electionDifficulty"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                },
                ""euroPerEnergy"": {
                    ""threshold"": 2,
                    ""authorizedKeys"": [0, 1]
                }
            },
            ""level1Keys"": {
                ""keys"": [
                    {
                        ""verifyKey"": ""12f6025af2762f500162d9f6b5558b698bae0c126725d499262d41aabe7ec0b3"",
                        ""schemeId"": ""Ed25519""
                    },
                    {
                        ""verifyKey"": ""bad22702a6cc31b3a6019660134faec718d44ecc3befe1078a27b5468e1db151"",
                        ""schemeId"": ""Ed25519""
                    }
                ],
                ""threshold"": 2
            }
        }";

        var result = JsonSerializer.Deserialize<UpdateKeysCollectionV0>(json, _serializerOptions)!;
        result.RootKeys.Threshold.Should().Be(2);
        result.RootKeys.Keys.Length.Should().Be(2);
        result.RootKeys.Keys[0].SchemeId.Should().Be("Ed25519");
        result.RootKeys.Keys[0].VerifyKey.Should().Be("0e87edd19aea0fe6bf70aaf7c4b27b28710d3f80f3c459d0fd4d1b56b82768d0");
        result.Level1Keys.Threshold.Should().Be(2);
        result.Level1Keys.Keys.Length.Should().Be(2);
        result.Level1Keys.Keys[0].SchemeId.Should().Be("Ed25519");
        result.Level1Keys.Keys[0].VerifyKey.Should().Be("12f6025af2762f500162d9f6b5558b698bae0c126725d499262d41aabe7ec0b3");
        result.Level2Keys.Keys[0].SchemeId.Should().Be("Ed25519");
        result.Level2Keys.Keys[0].VerifyKey.Should().Be("b709d87b3151890ca9635f81e321feff8a39560ad3aed71fa0272086f4e207b2");
        result.Level2Keys.Emergency.Threshold.Should().Be(2);
        result.Level2Keys.Emergency.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.Protocol.Threshold.Should().Be(2);
        result.Level2Keys.Protocol.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.ElectionDifficulty.Threshold.Should().Be(2);
        result.Level2Keys.ElectionDifficulty.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.EuroPerEnergy.Threshold.Should().Be(2);
        result.Level2Keys.EuroPerEnergy.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.MicroGTUPerEuro.Threshold.Should().Be(2);
        result.Level2Keys.MicroGTUPerEuro.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.FoundationAccount.Threshold.Should().Be(2);
        result.Level2Keys.FoundationAccount.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.MintDistribution.Threshold.Should().Be(2);
        result.Level2Keys.MintDistribution.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.TransactionFeeDistribution.Threshold.Should().Be(2);
        result.Level2Keys.TransactionFeeDistribution.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.ParamGASRewards.Threshold.Should().Be(2);
        result.Level2Keys.ParamGASRewards.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.BakerStakeThreshold.Threshold.Should().Be(2);
        result.Level2Keys.BakerStakeThreshold.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.AddAnonymityRevoker.Threshold.Should().Be(2);
        result.Level2Keys.AddAnonymityRevoker.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.AddIdentityProvider.Threshold.Should().Be(2);
        result.Level2Keys.AddIdentityProvider.AuthorizedKeys.Should().Equal(0, 1);
    }
    
    [Fact]
    public void Deserialize_V1()
    {

        var json = @"{
            ""rootKeys"": {
               ""keys"": [
                  {
                     ""verifyKey"": ""68acf950b120e1d6687d14522e03128a4a2d46506700d0ecfaac4817f607cd01"",
                     ""schemeId"": ""Ed25519""
                  },
                  {
                     ""verifyKey"": ""6f635df513501846e78ca8267f3eaf4f5f115572efe14e5f84031c8b1dd34115"",
                     ""schemeId"": ""Ed25519""
                  }               ],
               ""threshold"": 2
            },
            ""level2Keys"": {
               ""mintDistribution"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""cooldownParameters"": {
                  ""threshold"": 1,
                  ""authorizedKeys"": [0]
               },
               ""addAnonymityRevoker"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""transactionFeeDistribution"": {
                  ""threshold"": 1,
                  ""authorizedKeys"": [0]
               },
               ""bakerStakeThreshold"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""microGTUPerEuro"": {
                  ""threshold"": 1,
                  ""authorizedKeys"": [1]
               },
               ""protocol"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""addIdentityProvider"": {
                  ""threshold"": 1,
                  ""authorizedKeys"": [0]
               },
               ""paramGASRewards"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""emergency"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""keys"": [
                  {
                     ""verifyKey"": ""2c16896e51f67d2139fce0b12b7317b64b79505c8c90dafd2978759710985cc0"",
                     ""schemeId"": ""Ed25519""
                  },
                  {
                     ""verifyKey"": ""a65f93b6d660576d8534d8362f1ed4f12470435af01a52dbdb6fe9e81b6611a2"",
                     ""schemeId"": ""Ed25519""
                  }
               ],
               ""timeParameters"": {
                  ""threshold"": 1,
                  ""authorizedKeys"": [0]
               },
               ""foundationAccount"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""electionDifficulty"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               },
               ""euroPerEnergy"": {
                  ""threshold"": 2,
                  ""authorizedKeys"": [0, 1]
               }
            },
            ""level1Keys"": {
               ""keys"": [
                  {
                     ""verifyKey"": ""7851f3db9862a50caff06620f69e87ecaa3d1c34f70647b48445bb17aaa44255"",
                     ""schemeId"": ""Ed25519""
                  },
                  {
                     ""verifyKey"": ""e0de1bea464213a393a36810cf001cba10010e140e518de7e6658ac088b0e3b3"",
                     ""schemeId"": ""Ed25519""
                  }
               ],
               ""threshold"": 2
            }
        }";

        var result = JsonSerializer.Deserialize<UpdateKeysCollectionV1>(json, _serializerOptions)!;
        result.RootKeys.Threshold.Should().Be(2);
        result.RootKeys.Keys.Length.Should().Be(2);
        result.RootKeys.Keys[0].SchemeId.Should().Be("Ed25519");
        result.RootKeys.Keys[0].VerifyKey.Should().Be("68acf950b120e1d6687d14522e03128a4a2d46506700d0ecfaac4817f607cd01");
        result.Level1Keys.Threshold.Should().Be(2);
        result.Level1Keys.Keys.Length.Should().Be(2);
        result.Level1Keys.Keys[0].SchemeId.Should().Be("Ed25519");
        result.Level1Keys.Keys[0].VerifyKey.Should().Be("7851f3db9862a50caff06620f69e87ecaa3d1c34f70647b48445bb17aaa44255");
        result.Level2Keys.Keys.Length.Should().Be(2);
        result.Level2Keys.Keys[0].SchemeId.Should().Be("Ed25519");
        result.Level2Keys.Keys[0].VerifyKey.Should().Be("2c16896e51f67d2139fce0b12b7317b64b79505c8c90dafd2978759710985cc0");
        result.Level2Keys.Emergency.Threshold.Should().Be(2);
        result.Level2Keys.Emergency.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.Protocol.Threshold.Should().Be(2);
        result.Level2Keys.Protocol.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.ElectionDifficulty.Threshold.Should().Be(2);
        result.Level2Keys.ElectionDifficulty.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.EuroPerEnergy.Threshold.Should().Be(2);
        result.Level2Keys.EuroPerEnergy.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.MicroGTUPerEuro.Threshold.Should().Be(1);
        result.Level2Keys.MicroGTUPerEuro.AuthorizedKeys.Should().Equal(1);
        result.Level2Keys.FoundationAccount.Threshold.Should().Be(2);
        result.Level2Keys.FoundationAccount.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.MintDistribution.Threshold.Should().Be(2);
        result.Level2Keys.MintDistribution.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.TransactionFeeDistribution.Threshold.Should().Be(1);
        result.Level2Keys.TransactionFeeDistribution.AuthorizedKeys.Should().Equal(0);
        result.Level2Keys.ParamGASRewards.Threshold.Should().Be(2);
        result.Level2Keys.ParamGASRewards.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.BakerStakeThreshold.Threshold.Should().Be(2);
        result.Level2Keys.BakerStakeThreshold.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.AddAnonymityRevoker.Threshold.Should().Be(2);
        result.Level2Keys.AddAnonymityRevoker.AuthorizedKeys.Should().Equal(0, 1);
        result.Level2Keys.AddIdentityProvider.Threshold.Should().Be(1);
        result.Level2Keys.AddIdentityProvider.AuthorizedKeys.Should().Equal(0);
        result.Level2Keys.CooldownParameters.Threshold.Should().Be(1);
        result.Level2Keys.CooldownParameters.AuthorizedKeys.Should().Equal(0);
        result.Level2Keys.TimeParameters.Threshold.Should().Be(1);
        result.Level2Keys.TimeParameters.AuthorizedKeys.Should().Equal(0);
    }
}