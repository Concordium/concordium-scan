using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using FluentAssertions;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class BlockSummaryConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public BlockSummaryConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void Deserialize_V1()
    {
        var json = @"{
           ""protocolVersion"": 4,
           ""finalizationData"": null,
           ""specialEvents"": [],
           ""updates"": {
              ""chainParameters"": {
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
                 ""transactionCommissionLPool"": 0.1,
                 ""leverageBound"": {
                    ""denominator"": 1,
                    ""numerator"": 3
                 },
                 ""foundationAccountIndex"": 5,
                 ""finalizationCommissionLPool"": 1.0,
                 ""delegatorCooldown"": 7200,
                 ""bakingCommissionRange"": {
                    ""max"": 5.0e-2,
                    ""min"": 5.0e-2
                 },
                 ""bakingCommissionLPool"": 0.1,
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
              },
              ""keys"": {
                 ""rootKeys"": {
                    ""keys"": [],
                    ""threshold"": 0
                 },
                 ""level2Keys"": {
                    ""mintDistribution"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""cooldownParameters"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""addAnonymityRevoker"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""transactionFeeDistribution"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""bakerStakeThreshold"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""microGTUPerEuro"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""protocol"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""addIdentityProvider"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""paramGASRewards"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""emergency"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""keys"": [],
                    ""timeParameters"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""foundationAccount"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""electionDifficulty"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    },
                    ""euroPerEnergy"": {
                       ""threshold"": 0,
                       ""authorizedKeys"": []
                    }
                 },
                 ""level1Keys"": {
                    ""keys"": [],
                    ""threshold"": 0
                 }
              },
              ""updateQueues"": {
                 ""mintDistribution"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""cooldownParameters"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""rootKeys"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""addAnonymityRevoker"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""transactionFeeDistribution"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""level2Keys"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""microGTUPerEuro"": {
                    ""nextSequenceNumber"": 620,
                    ""queue"": []
                 },
                 ""protocol"": {
                    ""nextSequenceNumber"": 4,
                    ""queue"": []
                 },
                 ""addIdentityProvider"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""gasRewards"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""timeParameters"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""foundationAccount"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""electionDifficulty"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""euroPerEnergy"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""level1Keys"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 },
                 ""poolParameters"": {
                    ""nextSequenceNumber"": 1,
                    ""queue"": []
                 }
              }
           },
           ""transactionSummaries"": []
        }";
        
        var result = JsonSerializer.Deserialize<BlockSummaryBase>(json, _serializerOptions)!;
        var typed = result.Should().BeOfType<BlockSummaryV1>().Subject!;
        typed.TransactionSummaries.Should().BeEmpty();
        typed.SpecialEvents.Should().BeEmpty();
        typed.FinalizationData.Should().BeNull();
        typed.Updates.Should().NotBeNull();
    }

    [Fact]
    public void Deserialize_V0()
    {
        var json = @"{
            ""transactionSummaries"": [],
            ""specialEvents"": [],
            ""finalizationData"": null,
            ""updates"": {
                ""chainParameters"": {
                    ""minimumThresholdForBaking"": ""2500000000"",
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
                        ""denominator"": 1,
                        ""numerator"": 500000
                    },
                    ""foundationAccountIndex"": 13,
                    ""accountCreationLimit"": 10,
                    ""bakerCooldownEpochs"": 166,
                    ""electionDifficulty"": 2.5e-2,
                    ""euroPerEnergy"": {
                        ""denominator"": 50000,
                        ""numerator"": 1
                    }
                },
                ""keys"": {
                    ""rootKeys"": {
                        ""keys"": [],
                        ""threshold"": 0
                    },
                    ""level2Keys"": {
                        ""mintDistribution"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""addAnonymityRevoker"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""transactionFeeDistribution"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""bakerStakeThreshold"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""microGTUPerEuro"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""protocol"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""addIdentityProvider"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""paramGASRewards"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""emergency"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""keys"": [],
                        ""foundationAccount"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""electionDifficulty"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        },
                        ""euroPerEnergy"": {
                            ""threshold"": 0,
                            ""authorizedKeys"": []
                        }
                    },
                    ""level1Keys"": {
                        ""keys"": [],
                        ""threshold"": 0
                    }
                },
                ""updateQueues"": {
                    ""mintDistribution"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""rootKeys"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""addAnonymityRevoker"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""transactionFeeDistribution"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""bakerStakeThreshold"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""level2Keys"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""microGTUPerEuro"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""protocol"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""addIdentityProvider"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""gasRewards"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""foundationAccount"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""electionDifficulty"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""euroPerEnergy"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    },
                    ""level1Keys"": {
                        ""nextSequenceNumber"": 1,
                        ""queue"": []
                    }
                }
            }
        }";
        
        var result = JsonSerializer.Deserialize<BlockSummaryBase>(json, _serializerOptions)!;
        var typed = result.Should().BeOfType<BlockSummaryV0>().Subject!;
        typed.TransactionSummaries.Should().BeEmpty();
        typed.SpecialEvents.Should().BeEmpty();
        typed.FinalizationData.Should().BeNull();
        typed.Updates.Should().NotBeNull();
    }
}