using System.Text.Json;
using ConcordiumSdk.NodeApi.Types.JsonConverters;
using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TransactionTypeConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionTypeConverterTest()
    {
        _serializerOptions = new JsonSerializerOptions( );
        _serializerOptions.Converters.Add(new TransactionTypeConverter());
    }

    [Theory]
    [InlineData("\"initial\"", CredentialDeploymentTransactionType.Initial)]
    [InlineData("\"normal\"", CredentialDeploymentTransactionType.Normal)]
    [InlineData("null", null)]
    public void Deserialize_CredentialDeployment(string contentsString, CredentialDeploymentTransactionType? expected)
    {
        var json = $"{{\"contents\":{contentsString},\"type\":\"credentialDeploymentTransaction\"}}";
        var result = JsonSerializer.Deserialize<TransactionType>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionType<CredentialDeploymentTransactionType>>(result);
        Assert.Equal(expected, typed.Type);
    }
    
    [Theory]
    [InlineData(CredentialDeploymentTransactionType.Normal, "\"normal\"")]
    [InlineData(null, "null")]
    public void Serialize_CredentialDeployment(CredentialDeploymentTransactionType? type, string expectedContents)
    {
        // Based on the same dictionary as deserialize in the converter, so only single test + null done!
        var input = TransactionType.Get(type);
        var result = JsonSerializer.Serialize<TransactionType>(input, _serializerOptions);
        Assert.Equal($"{{\"contents\":{expectedContents},\"type\":\"credentialDeploymentTransaction\"}}", result);
    }

    [Theory]
    [InlineData("\"deployModule\"", AccountTransactionType.DeployModule)]    
    [InlineData("\"initContract\"", AccountTransactionType.InitializeSmartContractInstance)]    
    [InlineData("\"update\"", AccountTransactionType.UpdateSmartContractInstance)]    
    [InlineData("\"transfer\"", AccountTransactionType.SimpleTransfer)]    
    [InlineData("\"addBaker\"", AccountTransactionType.AddBaker)]    
    [InlineData("\"removeBaker\"", AccountTransactionType.RemoveBaker)]    
    [InlineData("\"updateBakerStake\"", AccountTransactionType.UpdateBakerStake)]    
    [InlineData("\"updateBakerRestakeEarnings\"", AccountTransactionType.UpdateBakerRestakeEarnings)]    
    [InlineData("\"updateBakerKeys\"", AccountTransactionType.UpdateBakerKeys)]    
    [InlineData("\"updateCredentialKeys\"", AccountTransactionType.UpdateCredentialKeys)]    
    [InlineData("\"encryptedAmountTransfer\"", AccountTransactionType.EncryptedTransfer)]    
    [InlineData("\"transferToEncrypted\"", AccountTransactionType.TransferToEncrypted)]    
    [InlineData("\"transferToPublic\"", AccountTransactionType.TransferToPublic)]    
    [InlineData("\"transferWithSchedule\"", AccountTransactionType.TransferWithSchedule)]    
    [InlineData("\"updateCredentials\"", AccountTransactionType.UpdateCredentials)]    
    [InlineData("\"registerData\"", AccountTransactionType.RegisterData)]    
    [InlineData("\"transferWithMemo\"", AccountTransactionType.SimpleTransferWithMemo)]    
    [InlineData("\"encryptedAmountTransferWithMemo\"", AccountTransactionType.EncryptedTransferWithMemo)]    
    [InlineData("\"transferWithScheduleAndMemo\"", AccountTransactionType.TransferWithScheduleWithMemo)]    
    [InlineData("null", null)]    
    public void Deserialize_AccountTransaction(string contentsString, AccountTransactionType? expected)
    {
        var json = $"{{\"contents\":{contentsString},\"type\":\"accountTransaction\"}}";
        var result = JsonSerializer.Deserialize<TransactionType>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionType<AccountTransactionType>>(result);
        Assert.Equal(expected, typed.Type);
    }

    [Theory]
    [InlineData(AccountTransactionType.SimpleTransfer, "\"transfer\"")]
    [InlineData(null, "null")]
    public void Serialize_AccountTransaction(AccountTransactionType? type, string expectedContents)
    {
        // Based on the same dictionary as deserialize in the converter, so only single test + null done!
        var input = TransactionType.Get(type);
        var result = JsonSerializer.Serialize<TransactionType>(input, _serializerOptions);
        Assert.Equal($"{{\"contents\":{expectedContents},\"type\":\"accountTransaction\"}}", result);
    }
    
    [Theory]
    [InlineData("\"updateProtocol\"", UpdateTransactionType.UpdateProtocol)]
    [InlineData("\"updateElectionDifficulty\"", UpdateTransactionType.UpdateElectionDifficulty)]
    [InlineData("\"updateEuroPerEnergy\"", UpdateTransactionType.UpdateEuroPerEnergy)]
    [InlineData("\"updateMicroGTUPerEuro\"", UpdateTransactionType.UpdateMicroGtuPerEuro)]
    [InlineData("\"updateFoundationAccount\"", UpdateTransactionType.UpdateFoundationAccount)]
    [InlineData("\"updateMintDistribution\"", UpdateTransactionType.UpdateMintDistribution)]
    [InlineData("\"updateTransactionFeeDistribution\"", UpdateTransactionType.UpdateTransactionFeeDistribution)]
    [InlineData("\"updateGASRewards\"", UpdateTransactionType.UpdateGasRewards)]
    [InlineData("\"updateBakerStakeThreshold\"", UpdateTransactionType.UpdateBakerStakeThreshold)]
    [InlineData("\"updateAddAnonymityRevoker\"", UpdateTransactionType.UpdateAddAnonymityRevoker)]
    [InlineData("\"updateAddIdentityProvider\"", UpdateTransactionType.UpdateAddIdentityProvider)]
    [InlineData("\"updateRootKeys\"", UpdateTransactionType.UpdateRootKeys)]
    [InlineData("\"updateLevel1Keys\"", UpdateTransactionType.UpdateLevel1Keys)]
    [InlineData("\"updateLevel2Keys\"", UpdateTransactionType.UpdateLevel2Keys)]
    [InlineData("null", null)]
    public void Deserialize_Update(string contentsString, UpdateTransactionType? expected)
    {
        var json = $"{{\"contents\":{contentsString},\"type\":\"updateTransaction\"}}";
        var result = JsonSerializer.Deserialize<TransactionType>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionType<UpdateTransactionType>>(result);
        Assert.Equal(expected, typed.Type);
    }
    
    [Theory]
    [InlineData(UpdateTransactionType.UpdateAddIdentityProvider, "\"updateAddIdentityProvider\"")]
    [InlineData(null, "null")]
    public void Serialize_Update(UpdateTransactionType? type, string expectedContents)
    {
        // Based on the same dictionary as deserialize in the converter, so only single test + null done!
        var input = TransactionType.Get(type);
        var result = JsonSerializer.Serialize<TransactionType>(input, _serializerOptions);
        Assert.Equal($"{{\"contents\":{expectedContents},\"type\":\"updateTransaction\"}}", result);
    }
}