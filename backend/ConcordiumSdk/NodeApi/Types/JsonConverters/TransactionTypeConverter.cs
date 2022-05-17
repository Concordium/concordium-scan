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
            { AccountTransactionType.ConfigureBaker, "configureBaker" },
            { AccountTransactionType.ConfigureDelegation, "configureDelegation" },
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
            { UpdateTransactionType.UpdatePoolParameters , "updatePoolParameters" },
            { UpdateTransactionType.UpdateCooldownParameters , "updateCooldownParameters" },
            { UpdateTransactionType.UpdateTimeParameters , "updateTimeParameters" },
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
            EnsureTokenType(reader, JsonTokenType.String, JsonTokenType.Null);
            var value = reader.GetString();
            
            if (key == "type") typeString = value;
            else if (key == "contents") contentsString = value;
            
            reader.Read();
        }

        if (typeString == "accountTransaction")
            return TransactionType.Get(contentsString != null ? _mapStringToAccountTransactionType[contentsString] : null);
        if (typeString == "credentialDeploymentTransaction")
            return TransactionType.Get(contentsString != null ? _mapStringToCredentialDeploymentTransactionType[contentsString] : null);
        if (typeString == "updateTransaction")
            return TransactionType.Get(contentsString != null ? _mapStringToUpdateTransactionType[contentsString] : null);
        throw new NotImplementedException();
    }

    private static void EnsureTokenType(Utf8JsonReader reader, params JsonTokenType[] expectedTokenTypes)
    {
        if (!expectedTokenTypes.Contains(reader.TokenType))
            throw new JsonException($"Must be {string.Join(" or ", expectedTokenTypes)}.");
    }

    public override void Write(Utf8JsonWriter writer, TransactionType value, JsonSerializerOptions options)
    {
        string? contentsString;
        string typeString;
        if (value is TransactionType<AccountTransactionType> accountTransaction)
        {
            contentsString = accountTransaction.Type.HasValue
                ? _mapAccountTransactionTypeToString[accountTransaction.Type.Value]
                : null;
            typeString = "accountTransaction";
        }
        else if (value is TransactionType<CredentialDeploymentTransactionType> credentialDeploymentTransaction)
        {
            contentsString = credentialDeploymentTransaction.Type.HasValue
                ? _mapCredentialDeploymentTransactionTypeToString[credentialDeploymentTransaction.Type.Value]
                : null;
            typeString = "credentialDeploymentTransaction";
        }
        else if (value is TransactionType<UpdateTransactionType> updateTransaction)
        {
            contentsString = updateTransaction.Type.HasValue
                ? _mapUpdateTransactionTypeToString[updateTransaction.Type.Value]
                : null;
            typeString = "updateTransaction";
        }
        else
            throw new InvalidOperationException("Cannot serialize that subtype");
        
        writer.WriteStartObject();
        if (contentsString != null)
            writer.WriteString("contents", contentsString);
        else
            writer.WriteNull("contents");
        writer.WriteString("type", typeString);
        writer.WriteEndObject();
    }
}