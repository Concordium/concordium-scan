using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

/// <summary>
/// Serialization format: {"contents":"[specific-transaction-type]","type":"[transaction-type]"}
/// </summary>
public class TransactionTypeConverter : JsonConverter<TransactionType>
{
    private readonly Dictionary<AccountTransactionType, string> _mapAccountTransactionTypeToString;
    private readonly Dictionary<string, AccountTransactionType> _mapStringToAccountTransactionType;
    private readonly Dictionary<CredentialDeploymentTransactionType, string> _mapCredentialDeploymentTransactionTypeToString;
    private readonly Dictionary<string, CredentialDeploymentTransactionType> _mapStringToCredentialDeploymentTransactionType;
    private readonly Dictionary<UpdateTransactionType, string> _mapUpdateTransactionTypeToString;
    private readonly Dictionary<string, UpdateTransactionType> _mapStringToUpdateTransactionType;

    public TransactionTypeConverter()
    {
        _mapAccountTransactionTypeToString = new()
        {
            { AccountTransactionType.DeployModule, "deployModule" },
            { AccountTransactionType.InitializeSmartContractInstance, "initContract" },
            { AccountTransactionType.UpdateSmartContractInstance, "update" },
            { AccountTransactionType.SimpleTransfer, "transfer" },
            { AccountTransactionType.AddBaker, "addBaker" },
            { AccountTransactionType.RemoveBaker, "removeBaker" },
            { AccountTransactionType.UpdateBakerStake, "updateBakerStake" },
            { AccountTransactionType.UpdateBakerRestakeEarnings, "updateBakerRestakeEarnings" },
            { AccountTransactionType.UpdateBakerKeys, "updateBakerKeys" },
            { AccountTransactionType.UpdateCredentialKeys, "updateCredentialKeys" },
            { AccountTransactionType.EncryptedTransfer, "encryptedAmountTransfer" },
            { AccountTransactionType.TransferToEncrypted, "transferToEncrypted" },
            { AccountTransactionType.TransferToPublic, "transferToPublic" },
            { AccountTransactionType.TransferWithSchedule, "transferWithSchedule" },
            { AccountTransactionType.UpdateCredentials, "updateCredentials" },
            { AccountTransactionType.RegisterData, "registerData" },
            { AccountTransactionType.SimpleTransferWithMemo, "transferWithMemo" },
            { AccountTransactionType.EncryptedTransferWithMemo, "encryptedAmountTransferWithMemo" },
            { AccountTransactionType.TransferWithScheduleWithMemo, "transferWithScheduleAndMemo" },
        };

        _mapStringToAccountTransactionType = _mapAccountTransactionTypeToString
            .ToDictionary(key => key.Value, element => element.Key);

        _mapCredentialDeploymentTransactionTypeToString = new()
        {
            { CredentialDeploymentTransactionType.Initial, "initial" },
            { CredentialDeploymentTransactionType.Normal, "normal" },
        };
        
        _mapStringToCredentialDeploymentTransactionType = _mapCredentialDeploymentTransactionTypeToString
            .ToDictionary(key => key.Value, element => element.Key);

        _mapUpdateTransactionTypeToString = new()
        {
            { UpdateTransactionType.UpdateProtocol, "updateProtocol" },
            { UpdateTransactionType.UpdateElectionDifficulty, "updateElectionDifficulty" },
            { UpdateTransactionType.UpdateEuroPerEnergy, "updateEuroPerEnergy" },
            { UpdateTransactionType.UpdateMicroGtuPerEuro, "updateMicroGTUPerEuro" },
            { UpdateTransactionType.UpdateFoundationAccount, "updateFoundationAccount" },
            { UpdateTransactionType.UpdateMintDistribution, "updateMintDistribution" },
            { UpdateTransactionType.UpdateTransactionFeeDistribution, "updateTransactionFeeDistribution" },
            { UpdateTransactionType.UpdateGasRewards, "updateGASRewards" },
            { UpdateTransactionType.UpdateBakerStakeThreshold, "updateBakerStakeThreshold" },
            { UpdateTransactionType.UpdateAddAnonymityRevoker, "updateAddAnonymityRevoker" },
            { UpdateTransactionType.UpdateAddIdentityProvider, "updateAddIdentityProvider" },
            { UpdateTransactionType.UpdateRootKeys, "updateRootKeys" },
            { UpdateTransactionType.UpdateLevel1Keys, "updateLevel1Keys" },
            { UpdateTransactionType.UpdateLevel2Keys , "updateLevel2Keys" },
        };
        
        _mapStringToUpdateTransactionType = _mapUpdateTransactionTypeToString
            .ToDictionary(key => key.Value, element => element.Key);
    }

    public override TransactionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);

        var typeString = "";
        var contentsString = "";
        
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            EnsureTokenType(reader, JsonTokenType.PropertyName);
            var key = reader.GetString()!;
            
            reader.Read();
            EnsureTokenType(reader, JsonTokenType.String);
            var value = reader.GetString()!;
            
            if (key == "type") typeString = value;
            else if (key == "contents") contentsString = value;
            
            reader.Read();
        }

        if (typeString == "accountTransaction")
            return TransactionType.Get(_mapStringToAccountTransactionType[contentsString]);
        if (typeString == "credentialDeploymentTransaction")
            return TransactionType.Get(_mapStringToCredentialDeploymentTransactionType[contentsString]);
        if (typeString == "updateTransaction")
            return TransactionType.Get(_mapStringToUpdateTransactionType[contentsString]);
        throw new NotImplementedException();
    }

    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (reader.TokenType != tokenType)
            throw new JsonException($"Must be {tokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, TransactionType value, JsonSerializerOptions options)
    {
        string contentsString;
        string typeString;
        if (value is TransactionType<AccountTransactionType> accountTransaction)
        {
            contentsString = _mapAccountTransactionTypeToString[accountTransaction.Type];
            typeString = "accountTransaction";
        }
        else if (value is TransactionType<CredentialDeploymentTransactionType> credentialDeploymentTransaction)
        {
            contentsString = _mapCredentialDeploymentTransactionTypeToString[credentialDeploymentTransaction.Type];
            typeString = "credentialDeploymentTransaction";
        }
        else if (value is TransactionType<UpdateTransactionType> updateTransaction)
        {
            contentsString = _mapUpdateTransactionTypeToString[updateTransaction.Type];
            typeString = "updateTransaction";
        }
        else
            throw new InvalidOperationException("Cannot serialize that subtype");
        
        writer.WriteStartObject();
        writer.WriteString("contents", contentsString);
        writer.WriteString("type", typeString);
        writer.WriteEndObject();
    }
}