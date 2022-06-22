using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class UpdatePayloadConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public UpdatePayloadConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void RoundTrip_ProtocolUpdate()
    {
        var json = " {\"updateType\": \"protocol\", \"update\": {\"message\": \"Enable transfer memos\", \"specificationURL\": \"https://github.com/Concordium/concordium-update-proposals/blob/main/updates/P2.txt\", \"specificationHash\": \"9b1f206bbe230fef248c9312805460b4f1b05c1ef3964946981a8d4abb58b923\", \"specificationAuxiliaryData\": \"0b4f\"}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<ProtocolUpdatePayload>(deserialized);
        typed.Content.Message.Should().Be("Enable transfer memos");
        typed.Content.SpecificationURL.Should().Be("https://github.com/Concordium/concordium-update-proposals/blob/main/updates/P2.txt");
        typed.Content.SpecificationHash.Should().Be("9b1f206bbe230fef248c9312805460b4f1b05c1ef3964946981a8d4abb58b923");
        typed.Content.SpecificationAuxiliaryData.Should().Be(BinaryData.FromHexString("0b4f"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_ElectionDifficulty()
    {
        var json = "{\"updateType\": \"electionDifficulty\", \"update\": 0.025}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<ElectionDifficultyUpdatePayload>(deserialized);
        typed.ElectionDifficulty.Should().Be(0.025m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_EuroPerEnergy()
    {
        var json = "{\"updateType\": \"euroPerEnergy\", \"update\": {\"numerator\": 1, \"denominator\": 50000}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<EuroPerEnergyUpdatePayload>(deserialized);
        typed.Content.Numerator.Should().Be(1);
        typed.Content.Denominator.Should().Be(50000);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_MicroGtuPerEuro()
    {
        var json = "{\"updateType\": \"microGTUPerEuro\", \"update\": {\"numerator\": 500001, \"denominator\": 1}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<MicroGtuPerEuroUpdatePayload>(deserialized);
        typed.Content.Numerator.Should().Be(500001);
        typed.Content.Denominator.Should().Be(1);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_FoundationAccount()
    {
        var json = "{\"updateType\": \"foundationAccount\", \"update\": \"3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi\"}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<FoundationAccountUpdatePayload>(deserialized);
        typed.Account.Should().Be(new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_MintDistribution()
    {
        var json = "{\"updateType\": \"mintDistribution\", \"update\": {\"mintPerSlot\": 0.000000000755560725, \"bakingReward\": 0.60001, \"finalizationReward\": 0.30002}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<MintDistributionV0UpdatePayload>(deserialized);
        typed.Content.MintPerSlot.Should().Be(0.000000000755560725m);
        typed.Content.BakingReward.Should().Be(0.60001m);
        typed.Content.FinalizationReward.Should().Be(0.30002m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_TransactionFeeDistribution()
    {
        var json = "{\"updateType\": \"transactionFeeDistribution\", \"update\": {\"baker\": 0.44, \"gasAccount\": 0.46001}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionFeeDistributionUpdatePayload>(deserialized);
        typed.Content.Baker.Should().Be(0.44m);
        typed.Content.GasAccount.Should().Be(0.46001m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_GasRewards()
    {
        var json = "{\"updateType\": \"gASRewards\", \"update\": {\"baker\": 0.25, \"chainUpdate\": 0.0050, \"accountCreation\": 0.020, \"finalizationProof\": 0.0050}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<GasRewardsUpdatePayload>(deserialized);
        typed.Content.Baker.Should().Be(0.25m);
        typed.Content.FinalizationProof.Should().Be(0.0050m);
        typed.Content.AccountCreation.Should().Be(0.020m);
        typed.Content.ChainUpdate.Should().Be(0.0050m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    /// <summary>
    /// This test can be removed once Concordium Nodes have been upgraded to at least version 4.0
    /// on both test- and mainnet.
    /// </summary>
    [Fact]
    public void RoundTrip_BakerStakeThreshold_ConcordiumNodeVersion3()
    {
        var json = "{\"updateType\": \"bakerStakeThreshold\", \"update\": \"14000000000\"}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<BakerStakeThresholdUpdatePayload>(deserialized);
        typed.Content.MinimumThresholdForBaking.Should().Be(CcdAmount.FromMicroCcd(14000000000));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_BakerStakeThreshold()
    {
        var json = @"{
                         ""updateType"": ""bakerStakeThreshold"",
                         ""update"": {
                             ""minimumThresholdForBaking"": ""14000000000""
                         }
                     }";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<BakerStakeThresholdUpdatePayload>(deserialized);
        typed.Content.MinimumThresholdForBaking.Should().Be(CcdAmount.FromMicroCcd(14000000000));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact(Skip = "No example of this update event was present on neither Mainnet nor Testnet at time of implementing serialization.")]
    public void RoundTrip_Root_RootKeysUpdate()
    {
        // See skip reason!
    }

    [Fact(Skip = "No example of this update event was present on neither Mainnet nor Testnet at time of implementing serialization.")]
    public void RoundTrip_Root_Level1KeysUpdate()
    {
        // See skip reason!
    }

    [Fact(Skip = "No example of this update event was present on neither Mainnet nor Testnet at time of implementing serialization.")]
    public void RoundTrip_Root_Level2KeysUpdate()
    {
        // See skip reason!
    }

    [Fact(Skip = "No example of this update event was present on neither Mainnet nor Testnet at time of implementing serialization.")]
    public void RoundTrip_Level1_level1KeysUpdate()
    {
        // See skip reason!
    }
    
    [Fact]
    public void RoundTrip_Level1_level2KeysUpdate()
    {
        var json = "{\"updateType\": \"level1\", \"update\": {\"typeOfUpdate\": \"level2KeysUpdate\", \"updatePayload\": {\"keys\": [{\"schemeId\": \"Ed25519\", \"verifyKey\": \"0fb2431e05980f143dd5b6e7e197aa3b8b4ab666b66be64c7f641dc5343e80a6\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"3b9022f1625f06795255489bfeb6ee6244a16991f4fa5cef9c4f4b6614eeb5cf\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"fff862a666372843e3d05514573a9ecf87e9258bda7a2df908962eec53611dfc\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"af65154d71176544869a01eee6195a3cd15a2e135bbf208b5f5f50867674fe07\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"be3322e2b3e7ff4f4ab1e9251bfc3e75024e2546b2aec36b5e754a7fc1b7629e\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"6d4924a5da84615352dd6e5f19bf58157838dbd4f2b9e67713fb3f6e39d32a44\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"8cba5dcc0ef47b69118dfacc695ee36faf845bb1963e80448297ce0087305d6d\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"563133c8df10a3a2d88522eb62629b9dac3dcafbe41a6f9419287755f93524ed\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"eb89caed1020683d47e33c4457aa2285f1ef8cda92f4cbba861aabf9d6508cda\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"3efd31536ee2b0453ea0553817f80ff1f94ae3d329a8d368dab998d04ba56e31\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"31f39c8851718bd104ce1d166d73305668cd2618ccf5f77f8b5206dc36005e90\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"cf78e2c726d31d3fe0ed3c32c44174de53a63885a0fd0f583a3d4777ea6ac39f\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"ff20982a805c847e6418f1b7cf199e20f1f7c6c7e0453f8342977671b323e134\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"802154292370cf24b1b408f1002d2ab3d7efea7fdec9bc8cbb1c6472421c9a49\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"8c094013f41d80b3c1d301a1c206b26a8865438985341946be6c0f35d5567743\"}], \"protocol\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"emergency\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"euroPerEnergy\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"microGTUPerEuro\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"paramGASRewards\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"mintDistribution\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"foundationAccount\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"electionDifficulty\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"addAnonymityRevoker\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"addIdentityProvider\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"bakerStakeThreshold\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"transactionFeeDistribution\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}}}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<Level1UpdatePayload>(deserialized);
        var typedContent = Assert.IsType<Level2KeysLevel1Update>(typed.Content);
        typedContent.Content.Should().NotBeNull();
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_Level1_level2KeysUpdateV1()
    {
        var json = @"{
                        ""updateType"": ""level1"",
                        ""update"": {
                            ""typeOfUpdate"": ""level2KeysUpdateV1"",
                            ""updatePayload"": {
                                ""mintDistribution"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        17,
                                        18,
                                        19
                                    ]
                                },
                                ""cooldownParameters"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        32,
                                        33,
                                        34
                                    ]
                                },
                                ""addAnonymityRevoker"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        37,
                                        38,
                                        39
                                    ]
                                },
                                ""transactionFeeDistribution"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        22,
                                        23,
                                        24
                                    ]
                                },
                                ""microGTUPerEuro"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        11,
                                        47,
                                        48
                                    ]
                                },
                                ""protocol"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        5,
                                        6,
                                        48
                                    ]
                                },
                                ""addIdentityProvider"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        42,
                                        43,
                                        44
                                    ]
                                },
                                ""paramGASRewards"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        27,
                                        28,
                                        47,
                                        48
                                    ]
                                },
                                ""emergency"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        0,
                                        1,
                                        48
                                    ]
                                },
                                ""keys"": [
                                    {
                                        ""verifyKey"": ""50ee8a6f47c4c3e7cf18e9472dd884c5e9999cc1e4b10e729b41202f4999c440"",
                                        ""schemeId"": ""Ed25519""
                                    },
                                    {
                                        ""verifyKey"": ""57ecaae59ce4356f967d68a436f49c71bd129d9cd08480d1eb90697c38e60267"",
                                        ""schemeId"": ""Ed25519""
                                    }
                                ],
                                ""timeParameters"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        32,
                                        33,
                                        34
                                    ]
                                },
                                ""foundationAccount"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        12,
                                        13,
                                        14
                                    ]
                                },
                                ""electionDifficulty"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        8,
                                        9
                                    ]
                                },
                                ""euroPerEnergy"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        8,
                                        9
                                    ]
                                },
                                ""poolParameters"": {
                                    ""threshold"": 1,
                                    ""authorizedKeys"": [
                                        32,
                                        48
                                    ]
                                }
                            }
                        }
                    }";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<Level1UpdatePayload>(deserialized);
        var typedContent = Assert.IsType<Level2KeysV1Level1Update>(typed.Content);
        typedContent.Content.Should().NotBeNull();
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AddAnonymityRevoker()
    {
        var json = "{\"updateType\": \"addAnonymityRevoker\", \"update\": {\"arIdentity\": 4, \"arPublicKey\": \"b14cbfe44a02c6b1f78711176d5f437295367aa4f2a8c2551ee10d25a03adc69d61a332a058971919dad7312e1fc94c585c0e880a7ab8b608de967314803baa728e73868f8f61603db63f0e0cf1afcec230cd37ac7f0981241a375929d32fe42\", \"arDescription\": {\"url\": \"http://example.com\", \"name\": \"ar4\", \"description\": \"lorem ipsum\"}}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<AddAnonymityRevokerUpdatePayload>(deserialized);
        typed.Content.ArIdentity.Should().Be(4);
        typed.Content.ArDescription.Should().Be(new ArOrIpDescription("ar4", "http://example.com", "lorem ipsum"));
        typed.Content.ArPublicKey.Should().Be("b14cbfe44a02c6b1f78711176d5f437295367aa4f2a8c2551ee10d25a03adc69d61a332a058971919dad7312e1fc94c585c0e880a7ab8b608de967314803baa728e73868f8f61603db63f0e0cf1afcec230cd37ac7f0981241a375929d32fe42");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AddIdentityProvider()
    {
        var json = "{\"updateType\": \"addIdentityProvider\", \"update\": {\"ipIdentity\": 3, \"ipVerifyKey\": \"97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb80000001eaeb946aec3adedf06ce29eed6049505914e4a73602b2300fd6509afcdef1a8d2ce16c49e0aa9f4ab6dcfbdf92da669b590c2b66da28aac7c0b1510b8f0d946ca45df080158b4b1ea35124b6c7e637d4b699456cd64e5ba5b52ec784709ab1d3f9611f8eb8ae7d4fa50488545e1539c92f5c1da53d79167f0b8328ea253f53013fa4023a3cc5e72136d99d681d4ad6d5aa777ce516b1b68544baed1bb97379c6b5cab21b2c0f457e6bb0094dc9f2e4e4ae66350d03b87923cc5d639bd939a4a0689d02698d0206bba4619ce4d93fa1f7efe27dd58c7f7d6b74b382f53d21c8672b6cc187ca41377a38399efa2129b686e8f7ba7f15479d9dec4bdaa8b4dbe01e2c310997e4af07156ef01ec75ea2bd8ca76151e76818dc078610e16c0a44ceb47a32137d3d4538cef40cc45d5d7fb4f8284e19dcdc77207059832916ed7b20572e977dee7eab55bf5627286aff2f158b0850559c08c3be81645e12f4e3197ccb06c7a82cc978eb57ca4859b87a28211e19498ced2adadccbc179a402d83ef665ea11612d2e6fd3e002f7e1f82db0849e2297d3729d2fa2aacfd24ed80266589a4af8ef138f6b4389d0b73901bf576677e8c48dffb0cec649a0ed942f79c4f2e0734e9788ac7c32f79835f68e260550296ea381d609f7d696de92ab7e8191f00c58b9cf26b758d0e3fc9551836a2fd15d9639f9a21714544be183afc2e0564583b654a1a87b52a8b702722993f0dcab4e38f5d90abc64e34bef1dd4eb888f8bfbf00baa26b317f63180103b18b56129f0111cf53309e51accf9277eba73555b1dcb3375b1916125a8531a381557ee55df6e6a80112058e22560c7031c1feb06c1bca9d86bd12630f1dce848c4a4b652fd1a4bcc43bebf10330108e77265eb508ed6dc2fc455f0066cae5aa662a6750799824faf0a8e108a43298cbc12d0b1ec1128862ed91e0899859d5e7f26652fdd52f0d7d964fa455a6ce0e538378d4f624c63b0a6523d847b4f0b1d2774400561c2599e8da3df8f80b525b5464fa230f7724af9c8332f4291ba2f398788a6f12e2ea9a1b847fa2c27f1bbc9e4e7e0cfdf677b74ea9c6309be6ccc365b0e6b849b5fe30828fe8b21436b9e3d268c27227e097f6684dca84df63c321d0606d680c291b94a88470239baebbedd410b4cafb2c60ce9625fc0b15cfed6dbd2d45a6f3acb4c21c688eda5339fe35982c76adcd00c9817ce9800e527d889a62a5538597bb464da510dc63c1f2672d720fcb566653814f9094be86538f0aba64fc674530c63d944b682cbeec02cd554f176f1ec64e5b9e1ece538d28a6c9a91de34a6b31df8b0895f93d5d7d15c014f3a126b07671048f8eeb5069d4e9877443b3958667922bf3726208460c294b0e869fd35454b1a304905485af35ed346a4377874d444710a1e3592f66d82eddb08f88ba9d2d1d54e139332a86ee9dd6a37693a250d3252817d81b2e2dbb38042da3e00cb9822c1ab12f38d64f99136b59329ac04719cdd0cf86f52a9af9014b72f5d9ed67567457e747d60d09b6acbe0bf76a817a1b7a5e96bd880dcb2722f4598b5dd5c40de7f78b14bfa1c8da0f5a3d6b0ff767949c5d34ef4eb92306a44e1c55d232cc8bda5c89c314654592a8027693e4b93df9d75c442d3d35d74c24ed11f4be003db249ae1109c3e94dacbe21276fc1ee242f9399b168ce3f8d938ac6de8002d7378fcec26e4b402cec177acc00c81a81e554fd2264e9281a11c51f63ec3510c94b24605884ecf850fce8373f86a68f28d356aa39e2d8f9c04ea420271d1fc343a5a0ec134eb0be95f4d21c8c224034464ec89a5f8f00db2c7f47d5c968fb0edead296ef31e0135f457810f98f899f58277c68bdeba53df979622a0e4386b7187ee088422860b96ae80e4e67c5eef3fb1b36430d69727c17420133b7abf5fe5ff1563bcb6368ab0a1505791a7469a45e22849807f8935404a904ff1ddede7ddc9967c7b68d7999eea79c0f516dc510dc35fabc2eafa1352318b9cca998b2083075331ff2f0000001ea017940f7393d3e7224f6047ec7b7e8edc0b6754f992debc1c928f99c8373fcccf8199307c2b62bd14406b5e993be90415c13feda5617a64d0bceb455c7b08a83da9b10928567b26e65c8aa4547d1609a3243085cfd929e54b671e79977fcff4ae8d2f62b989ac90ddfea5a912cf3012d60579e407aea2a47599228a7570d55909a1f3669fa3417a24bb7b0f1474c53d035c6446637a53029c0e49a53c0d845a68262d3a6fbd9d210ebaf546992b36f1cd72a490df15bbe29a61fc15efc21f5ead07c71633eb0abbb413e42d967a079f3546f66f129aa926fab72b2b51eb54e2126b77f054e5caf5ee241d263f2942050a94ae2548f4d9bda89a87ff2c81bf8499793426f84e53f52894460293a9126b95818b8d5d3927d975b82e5749ee1ed8b79b3ad7d2b1b17fb7856c73873ed9f31bffbf0dd60a55d919c3742bd5288045c7730ae7a779c5b2205e40f445738c73138413c97d9f4e649090a5ffeebfa1b9383819478989f19a4fe2c9b55e0418dcf88228a64df968eb075e24cb2c824105a978b365e31ca2c537a54be101d1e6d7534304ecb3039a1e6689d89e8b2fb05fa263eb460393f8535f7147217398b3fc05111a492432aaa39813431d8abc42ddf6dacb5206f34f1f10aa7b9e01179504fd693f960b35905b802d1fb7f233049bb878fc29c0d682c5cf437e870e40151c8887429cc9e193188b506b3891f444f94d097267bc3a30bff8fb899898432b62114f673c3844849cee3de7d6a9d7357a822b99e26a34709891a24eff4dbae06017aeeaa6c1023b5d05f5142ae6013df893aa82bb63fc203f9a0f751f8306931d36b54029608c68a09731a11bdbf2bf3a8fbdcbd8da186443ce6367fa2fb03f981342488f5dd3565c8b3f63a4847018535bd7134d755e50512f0e8c993b9dac83e00b6a561f6c0e7eee024bbd5cbb33a18a6bc85143d3212c99e4bb1940192ed74628f35ccfcefcb949291fe581eb524897e90e4c542968afcbd29325562f92570a6ad6ae02b87c8aa2121c5f5d74a9e2ce2efca60588477da56785dc7a1131120b8d2070907f3c42863acda1760bba9fb6bb5b6a504662e94005a96a94e795cb232bb3b72fc900818f9c7fd0d406489ab1e704ec19947b59b16665f35bbd1af60374ad200730c479e3b285762093f9449b08f2f1853797c8211a290c3209b305bd7328618c0bc647ebdbe80ed576662a8dff3187e3fca29653a4798e091176bbdc6a728029afbd8f8bcdabac8c9aed3281756708632468970836fdf4f1b2b6ce0d8d03ef96fd9dd22bed8723b989630bb3647f38ed44180e1e1c48daf3ed0b7fd30aa16352e593ba6504d19c5e1d2e87b80865266a4e76085b58efadf6063d1ab01f24ef2dce539c4d6da84a3f8ce59d13484830a86fae8e447826842cb0e823084b8ef28c5d8d0dc960c4a8f7c88ca53bcb9a6a16a0565f601cc0cac544d9e9ce9f31e8e9676798c76272e1fd6418e0af11911cf02222a98fa96c4bd41e9d3937ad677b5849286269afb1c6daef3c74b984e5fdd17ec87f59fd6b94b3b5dda50ff9ca97f20cd3a55f95746d79f824180a1c2b93be20ced95969d02b68b6f83b0fa7891bf44da452dddd891beae810fa8c02adf1b22a032c9a0ab0fde32a2b8cd22728fe182ed191d5af727ff725b674c136ef977382103cccc562b831ff40840e8f3ebb6405f6862060554930f448a8c287cffe0d07ad6a103ed5eeaf37d030e451b7c5dece8f45297a7de4e67d1d2a976cba31a9445379d64d9b8e10b6768b008f7406aac485a6fe2dc79208b2d389a4d82e188f2488c5b5699323cc2d19500991075f11374659d5bc3fc53f251c9656e59abf790863a3bf5094520cc024cce105df96d315022659b60ea9047d6049856c920eca8004c9c0912623fb4ae1893a573958748ff586a3f6e667f5f82d07ddf8526a8b32a34f66ab08a7363a98270fa6d1adc29506c798f3e9e02c10a70a5c6db2dcc8ab2edbe40660b6b555ae89c9b2a46df1c7e2830e70a5268560944081f7441e8b86909bc236fd9ab69ba7f830078745dfe81708967e6d48bd3a9817fc3fa9097a2381380391d47ae8baeeba181383642376527922d1cc3140cb9a072f2997e427cb1d22856bcdbb60e813310319a5eddcdb707d81ac3a611aaa170c9012e82a4000efcd1fb28918d88ac595b28f784dd84a1bb0631de13303872ca76ce25712a7c526c42c09ece052f1dc150e857a256a921134ca86f7162ddb89a78210f0035ae3087b6c76190c6d5b8c2df9cd804950b7abe28f4921e4ae0f66e081ec312236829eac407614fb2b876bd4ca399c7907323d80bb880cc3cde90218613229987eba5d620b8593a9525bfd88130c4ccfb572429015eff4c82a0e82b5a2a0e18e4919eef0531fcb6fb8b1063eee977ae3b01319c13a54d3e59c7ec5e7831d55ce1c75a2167e131dc128ea3690d293d8cd7ad2de9e3fc1855ad37b74dc75a8b4cb5a9b2be981dc5d2a5afc174c07b07af565205ba80300632d41fe0b255abb592d53da94e931e718aa7dfae22254368625c1841e2004e3840b44f0543891e82c522e15b105f1b8ce49a2cd2bcd7691d4efbc3d5de1efa8878c24d9fb0e4209278a700c76650f90b9813a08014d1709f4a524bdc9eeeb2b667c3361857b238f0a505f17a48f7a2d47e61643dac4d9207f5125ebbf150ec72c28650fe876813763eeb4b75cd20159b4e3d145f6e951fb333a5e4b7dc66d95fdff7f3bb5015ccfd6b3d827810f837ca96c33f323f2160f7bd0cc4d89f8136f5ea8b628d0ee990ddcd2fbddcffb173d1423c51dc6ebd53902e79451abd2744e1865382dd610b937e89dd0f6120f0c2d77f030b4c59b16e2714c2eb38c1e9f1f18fbfdb04532940915c10f1da7563726f446aa969cc1185bd4d9b54519bfb0461716a7219a60ae98ae27a74e94e247dbb1caef1747d853d688c7f986d3b47e39df75c3648126b3b89d8a4e3a6a74aa4975e976c9ab57b59d4bc0c488227525206d43e4da62f7d8150bcdb5d257ddddcaac6868e399d91291807210edd5d6e31cc94254c96913302f7b4af662baa46d455d684a0453957e2b1c95d66148454f8b98fd6d4595d994241e7de917446e4fd8b693f5b594c5cd8a25425d1ba102642d48824035709cc42350a128d7468f3d0099d1bd7a621914fae1a9a23e25fc03a52e6c71a7d88b6e924f6b93628545feb43942a4c7772231df13bcf7453968056df9282db1b218a93b60472271e6e5a4c3e0bc18a9a09dbe062ac45567f825c6d4df029b7214be428b03b1c6525ea16f103464e4978d4109fb699b19f1bf5eb5ceec0ca028307f9f1d157354396c124a7a252a942fccee2b51503b7a17e1f8f7ed6ad2acea4607960725920c3f04f192adaa079e06df139e25aa98213256eff9b29ee786b2ec4fb16c3a774ad6e72100c6fe11d84dd84c082cfc0057cd5d65affce28fd184aa2491ead1c78ebab4d9b627c412557b78f8a41e09556b74280b3fbfa7e943461b9bad3060d33b9290c6301a1b1372a0df9aeb7a0490afad22a624286a6fd5d5730f151651851bd95bea35e96019c8b2faf4091ec3ab488c24f16575cb1036058152e8ae1597aebb9d6dabd6cd9e06155254bbddda463299f9d0228406e263c0bee8b1e2dd68652f46e24f0f440440808e047b221d4922dad8b624f60b4b3d6fc169170acc7591ea6b559b187dde57a80ae212823b482870aac79e614f3aeed548d37d403b648595153df4ba074f72c434fce0805253b05b8e5fdca6e7b9329cc357a21a24cba226ccfc5d10f254043ab0d7dbbb60bab8f99aba9ffe6f8c3d2ad28088086c467c5b1d8512f798b1c6753d10093111f2611092cb211c4d20cda27d2de39051dfcf386aa4460476e1210e04538adce13eea936185eee9642fe33c993096314fe7d94e52dd8ebf21e8e4bd0a52f387ce509b729269c0d9c047669d721969c1fca983a873d7bd27a0d93903ad660880d4dff38012e0d72c776c31d9b4b48db507451a1e5bc96f9b6297791014b71611633099c37a0ffd3203f0d9ecd2fb82dacd56ab9b620f403bb04daff48fcab7464624220435fe980cc891916a4488486b8f0d3807ab01321f8da9ee33cac203235fd97528d7d29fa46539011583e9391374dd0e131ec9d448cdfe8d2125cbf29cd41fd2224e83538d8811eb3af80d\", \"ipDescription\": {\"url\": \"https://www.digitaltrustsolutions.nl\", \"name\": \"Digital Trust Solutions TestNet\", \"description\": \"Identity verified by Digital Trust Solutions on behalf of Concordium\"}, \"ipCdiVerifyKey\": \"534858c8990f225b34be324c74c03ce8745080d5d5ea4fde2468157b4892b690\"}}";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<AddIdentityProviderUpdatePayload>(deserialized);
        typed.Content.IpIdentity.Should().Be(3);
        typed.Content.IpDescription.Should().Be(new ArOrIpDescription("Digital Trust Solutions TestNet", "https://www.digitaltrustsolutions.nl", "Identity verified by Digital Trust Solutions on behalf of Concordium"));
        typed.Content.IpVerifyKey.Should().Be("97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb80000001eaeb946aec3adedf06ce29eed6049505914e4a73602b2300fd6509afcdef1a8d2ce16c49e0aa9f4ab6dcfbdf92da669b590c2b66da28aac7c0b1510b8f0d946ca45df080158b4b1ea35124b6c7e637d4b699456cd64e5ba5b52ec784709ab1d3f9611f8eb8ae7d4fa50488545e1539c92f5c1da53d79167f0b8328ea253f53013fa4023a3cc5e72136d99d681d4ad6d5aa777ce516b1b68544baed1bb97379c6b5cab21b2c0f457e6bb0094dc9f2e4e4ae66350d03b87923cc5d639bd939a4a0689d02698d0206bba4619ce4d93fa1f7efe27dd58c7f7d6b74b382f53d21c8672b6cc187ca41377a38399efa2129b686e8f7ba7f15479d9dec4bdaa8b4dbe01e2c310997e4af07156ef01ec75ea2bd8ca76151e76818dc078610e16c0a44ceb47a32137d3d4538cef40cc45d5d7fb4f8284e19dcdc77207059832916ed7b20572e977dee7eab55bf5627286aff2f158b0850559c08c3be81645e12f4e3197ccb06c7a82cc978eb57ca4859b87a28211e19498ced2adadccbc179a402d83ef665ea11612d2e6fd3e002f7e1f82db0849e2297d3729d2fa2aacfd24ed80266589a4af8ef138f6b4389d0b73901bf576677e8c48dffb0cec649a0ed942f79c4f2e0734e9788ac7c32f79835f68e260550296ea381d609f7d696de92ab7e8191f00c58b9cf26b758d0e3fc9551836a2fd15d9639f9a21714544be183afc2e0564583b654a1a87b52a8b702722993f0dcab4e38f5d90abc64e34bef1dd4eb888f8bfbf00baa26b317f63180103b18b56129f0111cf53309e51accf9277eba73555b1dcb3375b1916125a8531a381557ee55df6e6a80112058e22560c7031c1feb06c1bca9d86bd12630f1dce848c4a4b652fd1a4bcc43bebf10330108e77265eb508ed6dc2fc455f0066cae5aa662a6750799824faf0a8e108a43298cbc12d0b1ec1128862ed91e0899859d5e7f26652fdd52f0d7d964fa455a6ce0e538378d4f624c63b0a6523d847b4f0b1d2774400561c2599e8da3df8f80b525b5464fa230f7724af9c8332f4291ba2f398788a6f12e2ea9a1b847fa2c27f1bbc9e4e7e0cfdf677b74ea9c6309be6ccc365b0e6b849b5fe30828fe8b21436b9e3d268c27227e097f6684dca84df63c321d0606d680c291b94a88470239baebbedd410b4cafb2c60ce9625fc0b15cfed6dbd2d45a6f3acb4c21c688eda5339fe35982c76adcd00c9817ce9800e527d889a62a5538597bb464da510dc63c1f2672d720fcb566653814f9094be86538f0aba64fc674530c63d944b682cbeec02cd554f176f1ec64e5b9e1ece538d28a6c9a91de34a6b31df8b0895f93d5d7d15c014f3a126b07671048f8eeb5069d4e9877443b3958667922bf3726208460c294b0e869fd35454b1a304905485af35ed346a4377874d444710a1e3592f66d82eddb08f88ba9d2d1d54e139332a86ee9dd6a37693a250d3252817d81b2e2dbb38042da3e00cb9822c1ab12f38d64f99136b59329ac04719cdd0cf86f52a9af9014b72f5d9ed67567457e747d60d09b6acbe0bf76a817a1b7a5e96bd880dcb2722f4598b5dd5c40de7f78b14bfa1c8da0f5a3d6b0ff767949c5d34ef4eb92306a44e1c55d232cc8bda5c89c314654592a8027693e4b93df9d75c442d3d35d74c24ed11f4be003db249ae1109c3e94dacbe21276fc1ee242f9399b168ce3f8d938ac6de8002d7378fcec26e4b402cec177acc00c81a81e554fd2264e9281a11c51f63ec3510c94b24605884ecf850fce8373f86a68f28d356aa39e2d8f9c04ea420271d1fc343a5a0ec134eb0be95f4d21c8c224034464ec89a5f8f00db2c7f47d5c968fb0edead296ef31e0135f457810f98f899f58277c68bdeba53df979622a0e4386b7187ee088422860b96ae80e4e67c5eef3fb1b36430d69727c17420133b7abf5fe5ff1563bcb6368ab0a1505791a7469a45e22849807f8935404a904ff1ddede7ddc9967c7b68d7999eea79c0f516dc510dc35fabc2eafa1352318b9cca998b2083075331ff2f0000001ea017940f7393d3e7224f6047ec7b7e8edc0b6754f992debc1c928f99c8373fcccf8199307c2b62bd14406b5e993be90415c13feda5617a64d0bceb455c7b08a83da9b10928567b26e65c8aa4547d1609a3243085cfd929e54b671e79977fcff4ae8d2f62b989ac90ddfea5a912cf3012d60579e407aea2a47599228a7570d55909a1f3669fa3417a24bb7b0f1474c53d035c6446637a53029c0e49a53c0d845a68262d3a6fbd9d210ebaf546992b36f1cd72a490df15bbe29a61fc15efc21f5ead07c71633eb0abbb413e42d967a079f3546f66f129aa926fab72b2b51eb54e2126b77f054e5caf5ee241d263f2942050a94ae2548f4d9bda89a87ff2c81bf8499793426f84e53f52894460293a9126b95818b8d5d3927d975b82e5749ee1ed8b79b3ad7d2b1b17fb7856c73873ed9f31bffbf0dd60a55d919c3742bd5288045c7730ae7a779c5b2205e40f445738c73138413c97d9f4e649090a5ffeebfa1b9383819478989f19a4fe2c9b55e0418dcf88228a64df968eb075e24cb2c824105a978b365e31ca2c537a54be101d1e6d7534304ecb3039a1e6689d89e8b2fb05fa263eb460393f8535f7147217398b3fc05111a492432aaa39813431d8abc42ddf6dacb5206f34f1f10aa7b9e01179504fd693f960b35905b802d1fb7f233049bb878fc29c0d682c5cf437e870e40151c8887429cc9e193188b506b3891f444f94d097267bc3a30bff8fb899898432b62114f673c3844849cee3de7d6a9d7357a822b99e26a34709891a24eff4dbae06017aeeaa6c1023b5d05f5142ae6013df893aa82bb63fc203f9a0f751f8306931d36b54029608c68a09731a11bdbf2bf3a8fbdcbd8da186443ce6367fa2fb03f981342488f5dd3565c8b3f63a4847018535bd7134d755e50512f0e8c993b9dac83e00b6a561f6c0e7eee024bbd5cbb33a18a6bc85143d3212c99e4bb1940192ed74628f35ccfcefcb949291fe581eb524897e90e4c542968afcbd29325562f92570a6ad6ae02b87c8aa2121c5f5d74a9e2ce2efca60588477da56785dc7a1131120b8d2070907f3c42863acda1760bba9fb6bb5b6a504662e94005a96a94e795cb232bb3b72fc900818f9c7fd0d406489ab1e704ec19947b59b16665f35bbd1af60374ad200730c479e3b285762093f9449b08f2f1853797c8211a290c3209b305bd7328618c0bc647ebdbe80ed576662a8dff3187e3fca29653a4798e091176bbdc6a728029afbd8f8bcdabac8c9aed3281756708632468970836fdf4f1b2b6ce0d8d03ef96fd9dd22bed8723b989630bb3647f38ed44180e1e1c48daf3ed0b7fd30aa16352e593ba6504d19c5e1d2e87b80865266a4e76085b58efadf6063d1ab01f24ef2dce539c4d6da84a3f8ce59d13484830a86fae8e447826842cb0e823084b8ef28c5d8d0dc960c4a8f7c88ca53bcb9a6a16a0565f601cc0cac544d9e9ce9f31e8e9676798c76272e1fd6418e0af11911cf02222a98fa96c4bd41e9d3937ad677b5849286269afb1c6daef3c74b984e5fdd17ec87f59fd6b94b3b5dda50ff9ca97f20cd3a55f95746d79f824180a1c2b93be20ced95969d02b68b6f83b0fa7891bf44da452dddd891beae810fa8c02adf1b22a032c9a0ab0fde32a2b8cd22728fe182ed191d5af727ff725b674c136ef977382103cccc562b831ff40840e8f3ebb6405f6862060554930f448a8c287cffe0d07ad6a103ed5eeaf37d030e451b7c5dece8f45297a7de4e67d1d2a976cba31a9445379d64d9b8e10b6768b008f7406aac485a6fe2dc79208b2d389a4d82e188f2488c5b5699323cc2d19500991075f11374659d5bc3fc53f251c9656e59abf790863a3bf5094520cc024cce105df96d315022659b60ea9047d6049856c920eca8004c9c0912623fb4ae1893a573958748ff586a3f6e667f5f82d07ddf8526a8b32a34f66ab08a7363a98270fa6d1adc29506c798f3e9e02c10a70a5c6db2dcc8ab2edbe40660b6b555ae89c9b2a46df1c7e2830e70a5268560944081f7441e8b86909bc236fd9ab69ba7f830078745dfe81708967e6d48bd3a9817fc3fa9097a2381380391d47ae8baeeba181383642376527922d1cc3140cb9a072f2997e427cb1d22856bcdbb60e813310319a5eddcdb707d81ac3a611aaa170c9012e82a4000efcd1fb28918d88ac595b28f784dd84a1bb0631de13303872ca76ce25712a7c526c42c09ece052f1dc150e857a256a921134ca86f7162ddb89a78210f0035ae3087b6c76190c6d5b8c2df9cd804950b7abe28f4921e4ae0f66e081ec312236829eac407614fb2b876bd4ca399c7907323d80bb880cc3cde90218613229987eba5d620b8593a9525bfd88130c4ccfb572429015eff4c82a0e82b5a2a0e18e4919eef0531fcb6fb8b1063eee977ae3b01319c13a54d3e59c7ec5e7831d55ce1c75a2167e131dc128ea3690d293d8cd7ad2de9e3fc1855ad37b74dc75a8b4cb5a9b2be981dc5d2a5afc174c07b07af565205ba80300632d41fe0b255abb592d53da94e931e718aa7dfae22254368625c1841e2004e3840b44f0543891e82c522e15b105f1b8ce49a2cd2bcd7691d4efbc3d5de1efa8878c24d9fb0e4209278a700c76650f90b9813a08014d1709f4a524bdc9eeeb2b667c3361857b238f0a505f17a48f7a2d47e61643dac4d9207f5125ebbf150ec72c28650fe876813763eeb4b75cd20159b4e3d145f6e951fb333a5e4b7dc66d95fdff7f3bb5015ccfd6b3d827810f837ca96c33f323f2160f7bd0cc4d89f8136f5ea8b628d0ee990ddcd2fbddcffb173d1423c51dc6ebd53902e79451abd2744e1865382dd610b937e89dd0f6120f0c2d77f030b4c59b16e2714c2eb38c1e9f1f18fbfdb04532940915c10f1da7563726f446aa969cc1185bd4d9b54519bfb0461716a7219a60ae98ae27a74e94e247dbb1caef1747d853d688c7f986d3b47e39df75c3648126b3b89d8a4e3a6a74aa4975e976c9ab57b59d4bc0c488227525206d43e4da62f7d8150bcdb5d257ddddcaac6868e399d91291807210edd5d6e31cc94254c96913302f7b4af662baa46d455d684a0453957e2b1c95d66148454f8b98fd6d4595d994241e7de917446e4fd8b693f5b594c5cd8a25425d1ba102642d48824035709cc42350a128d7468f3d0099d1bd7a621914fae1a9a23e25fc03a52e6c71a7d88b6e924f6b93628545feb43942a4c7772231df13bcf7453968056df9282db1b218a93b60472271e6e5a4c3e0bc18a9a09dbe062ac45567f825c6d4df029b7214be428b03b1c6525ea16f103464e4978d4109fb699b19f1bf5eb5ceec0ca028307f9f1d157354396c124a7a252a942fccee2b51503b7a17e1f8f7ed6ad2acea4607960725920c3f04f192adaa079e06df139e25aa98213256eff9b29ee786b2ec4fb16c3a774ad6e72100c6fe11d84dd84c082cfc0057cd5d65affce28fd184aa2491ead1c78ebab4d9b627c412557b78f8a41e09556b74280b3fbfa7e943461b9bad3060d33b9290c6301a1b1372a0df9aeb7a0490afad22a624286a6fd5d5730f151651851bd95bea35e96019c8b2faf4091ec3ab488c24f16575cb1036058152e8ae1597aebb9d6dabd6cd9e06155254bbddda463299f9d0228406e263c0bee8b1e2dd68652f46e24f0f440440808e047b221d4922dad8b624f60b4b3d6fc169170acc7591ea6b559b187dde57a80ae212823b482870aac79e614f3aeed548d37d403b648595153df4ba074f72c434fce0805253b05b8e5fdca6e7b9329cc357a21a24cba226ccfc5d10f254043ab0d7dbbb60bab8f99aba9ffe6f8c3d2ad28088086c467c5b1d8512f798b1c6753d10093111f2611092cb211c4d20cda27d2de39051dfcf386aa4460476e1210e04538adce13eea936185eee9642fe33c993096314fe7d94e52dd8ebf21e8e4bd0a52f387ce509b729269c0d9c047669d721969c1fca983a873d7bd27a0d93903ad660880d4dff38012e0d72c776c31d9b4b48db507451a1e5bc96f9b6297791014b71611633099c37a0ffd3203f0d9ecd2fb82dacd56ab9b620f403bb04daff48fcab7464624220435fe980cc891916a4488486b8f0d3807ab01321f8da9ee33cac203235fd97528d7d29fa46539011583e9391374dd0e131ec9d448cdfe8d2125cbf29cd41fd2224e83538d8811eb3af80d");
        typed.Content.IpCdiVerifyKey.Should().Be("534858c8990f225b34be324c74c03ce8745080d5d5ea4fde2468157b4892b690");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_TimeParametersCPV1()
    {
        var json = @"{
                         ""updateType"": ""timeParametersCPV1"",
                         ""update"": {
                             ""mintPerPayday"": 0.0002,
                             ""rewardPeriodLength"": 2
                         }
                     }";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<TimeParametersUpdatePayload>(deserialized);
        typed.Content.MintPerPayday.Should().Be(0.0002m);
        typed.Content.RewardPeriodLength.Should().Be(2);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_CooldownParametersCPV1()
    {
        var json = @"{
                         ""updateType"": ""cooldownParametersCPV1"",
                         ""update"": {
                             ""poolOwnerCooldown"": 7200,
                             ""delegatorCooldown"": 3800
                         }
                     }";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<CooldownParametersUpdatePayload>(deserialized);
        typed.Content.PoolOwnerCooldown.Should().Be(7200);
        typed.Content.DelegatorCooldown.Should().Be(3800);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_PoolParametersCPV1()
    {
        var json = @"{
                         ""updateType"": ""poolParametersCPV1"",
                         ""update"": {
                             ""capitalBound"": 0.4,
                             ""passiveBakingCommission"": 0.12,
                             ""leverageBound"": {
                                 ""denominator"": 1,
                                 ""numerator"": 3
                             },
                             ""passiveFinalizationCommission"": 1.0,
                             ""passiveTransactionCommission"": 0.12,
                             ""bakingCommissionRange"": {
                                 ""max"": 0.1,
                                 ""min"": 0.1
                             },
                             ""finalizationCommissionRange"": {
                                 ""max"": 1.0,
                                 ""min"": 1.0
                             },
                             ""transactionCommissionRange"": {
                                 ""max"": 0.1,
                                 ""min"": 0.1
                             },
                             ""minimumEquityCapital"": ""14000000000""
                         }
                     }";
        
        var deserialized = JsonSerializer.Deserialize<UpdatePayload>(json, _serializerOptions);
        var typed = Assert.IsType<PoolParametersUpdatePayload>(deserialized);
        typed.Content.CapitalBound.Should().Be(0.4m);
        typed.Content.PassiveBakingCommission.Should().Be(0.12m);
        typed.Content.LeverageBound.Denominator.Should().Be(1);
        typed.Content.LeverageBound.Numerator.Should().Be(3);
        typed.Content.PassiveFinalizationCommission.Should().Be(1.0m);
        typed.Content.PassiveTransactionCommission.Should().Be(0.12m);
        typed.Content.BakingCommissionRange.Min.Should().Be(0.1m);
        typed.Content.BakingCommissionRange.Max.Should().Be(0.1m);
        typed.Content.FinalizationCommissionRange.Min.Should().Be(1.0m);
        typed.Content.FinalizationCommissionRange.Max.Should().Be(1.0m);
        typed.Content.TransactionCommissionRange.Min.Should().Be(0.1m);
        typed.Content.TransactionCommissionRange.Max.Should().Be(0.1m);
        typed.Content.MinimumEquityCapital.Should().Be(CcdAmount.FromMicroCcd(14000000000));

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}