using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TransactionRejectReasonConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionRejectReasonConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }
    
    [Fact]
    public void RoundTrip_ModuleNotWf()
    {
        var json = "{\"tag\": \"ModuleNotWF\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<ModuleNotWf>(deserialized);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_ModuleHashAlreadyExists()
    {
        var json = "{\"tag\": \"ModuleHashAlreadyExists\", \"contents\": \"506dbcb455ce4c4c168fc9d87f3a305f4e163b186e1cfefeb6ce570d9324b19f\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<ModuleHashAlreadyExists>(deserialized);
        typed.Contents.Should().Be(new ModuleRef("506dbcb455ce4c4c168fc9d87f3a305f4e163b186e1cfefeb6ce570d9324b19f"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);

    }
    
    [Fact]
    public void RoundTrip_InvalidAccountReference()
    {
        var json = "{\"tag\": \"InvalidAccountReference\", \"contents\": \"3MGEAp5EJ8GyrvNNAZRoR9KMi4iuGJwU6fhpUD5TsGv6FDvXKZ\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<InvalidAccountReference>(deserialized);
        typed.Contents.Should().Be(new AccountAddress("3MGEAp5EJ8GyrvNNAZRoR9KMi4iuGJwU6fhpUD5TsGv6FDvXKZ"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_InvalidInitMethod()
    {
        var json = "{\"tag\": \"InvalidInitMethod\", \"contents\": [\"6fd0437383e141ad1da64fc56372fff87e6e673910d9cd0604584e8af1c704ae\", \"init_INDBankU8\"]}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<InvalidInitMethod>(deserialized);
        typed.ModuleRef.Should().Be(new ModuleRef("6fd0437383e141ad1da64fc56372fff87e6e673910d9cd0604584e8af1c704ae"));
        typed.InitName.Should().Be("init_INDBankU8");

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_InvalidReceiveMethod()
    {
        var json = "{\"tag\": \"InvalidReceiveMethod\", \"contents\": [\"ab2a61d0c503bd632fba480aeed131b09a31c8bdbc06ee3db0dba26fe8b1a9c3\", \"inventory.create\"]}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<InvalidReceiveMethod>(deserialized);
        typed.ModuleRef.Should().Be(new ModuleRef("ab2a61d0c503bd632fba480aeed131b09a31c8bdbc06ee3db0dba26fe8b1a9c3"));
        typed.ReceiveName.Should().Be("inventory.create");

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_InvalidModuleReference()
    {
        var json = "{\"tag\": \"InvalidModuleReference\", \"contents\": \"bc4375aa41393348e84429c8d379d5972a5265ef054b2f23e852a09f8af29ba8\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<InvalidModuleReference>(deserialized);
        typed.Contents.Should().Be(new ModuleRef("bc4375aa41393348e84429c8d379d5972a5265ef054b2f23e852a09f8af29ba8"));

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_InvalidContractAddress()
    {
        var json = "{\"tag\": \"InvalidContractAddress\", \"contents\": {\"subindex\": 20, \"index\": 354}}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<InvalidContractAddress>(deserialized);
        typed.Contents.Should().Be(new ContractAddress(354, 20));

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_RuntimeFailure()
    {
        var json = "{\"tag\": \"RuntimeFailure\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<RuntimeFailure>(deserialized);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_AmountTooLarge()
    {
        var json = "{\"tag\": \"AmountTooLarge\", \"contents\": [{\"address\": \"36Cas4nsMFefjtZZUZyxnUMD98rzgifzPPr84ymEc92kJuNE5n\", \"type\": \"AddressAccount\"}, \"2000000\"]}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<AmountTooLarge>(deserialized);
        typed.Address.Should().Be(new AccountAddress("36Cas4nsMFefjtZZUZyxnUMD98rzgifzPPr84ymEc92kJuNE5n"));
        typed.Amount.Should().Be(CcdAmount.FromMicroCcd(2000000));

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_SerializationFailure()
    {
        var json = "{\"tag\": \"SerializationFailure\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<SerializationFailure>(deserialized);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_OutOfEnergy()
    {
        var json = "{\"tag\": \"OutOfEnergy\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<OutOfEnergy>(deserialized);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_RejectedInit()
    {
        var json = "{\"tag\": \"RejectedInit\", \"rejectReason\": -2147483646}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<RejectedInit>(deserialized);
        typed.RejectReason.Should().Be(-2147483646);
    
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_RejectedReceive()
    {
        var json = "{\"contractAddress\": {\"subindex\": 0, \"index\": 54}, \"tag\": \"RejectedReceive\", \"receiveName\": \"CTS1-NFT.mint\", \"rejectReason\": -1, \"parameter\": \"00c320b41f1997accd5d21c6bf4992370948ed711435e0e2c9302def62afd1295f01020abf\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<RejectedReceive>(deserialized);
        typed.ContractAddress.Should().Be(new ContractAddress(54, 0));
        typed.RejectReason.Should().Be(-1);
        typed.ReceiveName.Should().Be("CTS1-NFT.mint");
        typed.Parameter.Should().Be(BinaryData.FromHexString("00c320b41f1997accd5d21c6bf4992370948ed711435e0e2c9302def62afd1295f01020abf"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_NonExistentRewardAccount()
    {
    }
    
    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_InvalidProof()
    {
    }
    
    [Fact]
    public void RoundTrip_AlreadyABaker()
    {
        var json = "{\"tag\": \"AlreadyABaker\", \"contents\": 700}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<AlreadyABaker>(deserialized);
        typed.Contents.Should().Be(700);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_NotABaker()
    {
    }

    [Fact]
    public void RoundTrip_InsufficientBalanceForBakerStake()
    {
        var json = "{\"tag\": \"InsufficientBalanceForBakerStake\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<InsufficientBalanceForBakerStake>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_StakeUnderMinimumThresholdForBaking()
    {
        var json = "{\"tag\": \"StakeUnderMinimumThresholdForBaking\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<StakeUnderMinimumThresholdForBaking>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_BakerInCooldown()
    {
        var json = "{\"tag\": \"BakerInCooldown\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<BakerInCooldown>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
  
    [Fact]
    public void RoundTrip_DuplicateAggregationKey()
    {
        var json = "{\"tag\": \"DuplicateAggregationKey\", \"contents\": \"98528ef89dc117f102ef3f089c81b92e4d945d22c0269269af6ef9f876d79e828b31b8b4b8cc3d9234c30e83bd79e20a0a807bc110f0ac9babae90cb6a8c6d0deb2e5627704b41bdd646a547895fd1f9f2a7b0dd4fb4e138356e91d002a28f83\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<DuplicateAggregationKey>(deserialized);
        typed.Contents.Should().Be("98528ef89dc117f102ef3f089c81b92e4d945d22c0269269af6ef9f876d79e828b31b8b4b8cc3d9234c30e83bd79e20a0a807bc110f0ac9babae90cb6a8c6d0deb2e5627704b41bdd646a547895fd1f9f2a7b0dd4fb4e138356e91d002a28f83");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_NonExistentCredentialID()
    {
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_KeyIndexAlreadyInUse()
    {
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_InvalidAccountThreshold()
    {
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_InvalidCredentialKeySignThreshold()
    {
    }

    [Fact]
    public void RoundTrip_InvalidEncryptedAmountTransferProof()
    {
        var json = "{\"tag\": \"InvalidEncryptedAmountTransferProof\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<InvalidEncryptedAmountTransferProof>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_InvalidTransferToPublicProof()
    {
        var json = "{\"tag\": \"InvalidTransferToPublicProof\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<InvalidTransferToPublicProof>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_EncryptedAmountSelfTransfer()
    {
        var json = "{\"tag\": \"EncryptedAmountSelfTransfer\", \"contents\": \"4EdBeGmpnQZWxaiig7FGEhWwmJurYmYsPWXo6owM4tU37eosam\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<EncryptedAmountSelfTransfer>(deserialized);
        typed.Contents.Should().Be(new AccountAddress("4EdBeGmpnQZWxaiig7FGEhWwmJurYmYsPWXo6owM4tU37eosam"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_InvalidIndexOnEncryptedTransfer()
    {
    }

    [Fact]
    public void RoundTrip_ZeroScheduledAmount()
    {
        var json = "{\"tag\": \"ZeroScheduledAmount\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<ZeroScheduledAmount>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_NonIncreasingSchedule()
    {
        var json = "{\"tag\": \"NonIncreasingSchedule\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<NonIncreasingSchedule>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_FirstScheduledReleaseExpired()
    {
        var json = "{\"tag\": \"FirstScheduledReleaseExpired\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<FirstScheduledReleaseExpired>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_ScheduledSelfTransfer()
    {
        var json = "{\"tag\": \"ScheduledSelfTransfer\", \"contents\": \"4EdBeGmpnQZWxaiig7FGEhWwmJurYmYsPWXo6owM4tU37eosam\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<ScheduledSelfTransfer>(deserialized);
        typed.Contents.Should().Be(new AccountAddress("4EdBeGmpnQZWxaiig7FGEhWwmJurYmYsPWXo6owM4tU37eosam"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_InvalidCredentials()
    {
        var json = "{\"tag\": \"InvalidCredentials\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<InvalidCredentials>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DuplicateCredIDs()
    {
        var json = "{\"tag\": \"DuplicateCredIDs\", \"contents\": [\"b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f\"]}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<DuplicateCredIDs>(deserialized);
        typed.Contents.Should().ContainSingle().Which.Should().Be("b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_NonExistentCredIDs()
    {
        var json = "{\"tag\": \"NonExistentCredIDs\", \"contents\": [\"8221a0f0694ec0b8da5924e546d3b56e8d1421471771fd88253d1e63869680987f9fbd5b0c40612daa67cc6e9834a667\"]}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        var typed = Assert.IsType<NonExistentCredIDs>(deserialized);
        typed.Contents.Should().ContainSingle().Which.Should().Be("8221a0f0694ec0b8da5924e546d3b56e8d1421471771fd88253d1e63869680987f9fbd5b0c40612daa67cc6e9834a667");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_RemoveFirstCredential()
    {
        var json = "{\"tag\": \"RemoveFirstCredential\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<RemoveFirstCredential>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact(Skip = "No example on chain at time of writing...")]
    public void RoundTrip_CredentialHolderDidNotSign()
    {
    }

    [Fact]
    public void RoundTrip_NotAllowedMultipleCredentials()
    {
        var json = "{\"tag\": \"NotAllowedMultipleCredentials\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<NotAllowedMultipleCredentials>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_NotAllowedToReceiveEncrypted()
    {
        var json = "{\"tag\": \"NotAllowedToReceiveEncrypted\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<NotAllowedToReceiveEncrypted>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_NotAllowedToHandleEncrypted()
    {
        var json = "{\"tag\": \"NotAllowedToHandleEncrypted\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionRejectReason>(json, _serializerOptions);
        Assert.IsType<NotAllowedToHandleEncrypted>(deserialized);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}