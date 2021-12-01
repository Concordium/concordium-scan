using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Types;
using Nonce = ConcordiumSdk.Types.Nonce;

namespace ConcordiumSdk.Transactions;

public class AccountTransactionService
{
    private readonly INodeClient _client;

    public AccountTransactionService(INodeClient client)
    {
        _client = client;
    }

    public async Task<string> SendAccountTransactionAsync(AccountAddress sender, IAccountTransactionPayload transactionPayload, ITransactionSigner signer)
    {
        var signatureCount = 1; // TODO!!!
        
        var nextAccountNonce = await _client.GetNextAccountNonceAsync(sender);

        var serializedTransactionType = new[] { (byte)transactionPayload.TransactionType };
        var serializedPayload = transactionPayload.SerializeToBytes();
        var serializedHeader = CreateSerializedHeader(sender, signatureCount, serializedPayload.Length, transactionPayload.GetBaseEnergyCost(), nextAccountNonce.Nonce);
        
        var serializedTransaction = serializedHeader.Concat(serializedTransactionType).Concat(serializedPayload).ToArray();

        var signDigest = SHA256.Create().ComputeHash(serializedTransaction);
        var signature = signer.Sign(signDigest);
        
        var serializedSignatures = new byte[6 + 64];
        serializedSignatures[0] = 1;  // [1 byte]  credential index data length
        serializedSignatures[1] = 0;  // [1 byte]  credential index 0
        serializedSignatures[2] = 1;  // [1 byte]  key index data length
        serializedSignatures[3] = 0;  // [1 byte]  key index 0
        serializedSignatures[4] = 0;  // [2 bytes] signature length: 64
        serializedSignatures[5] = 64; // -----
        signature.CopyTo(serializedSignatures, 6); // [64 bytes] signature
        
        var serializedBlockItemKind = new byte[] { 0 }; // AccountTransactionKind: 0, CredentialDeploymentKind: 1, UpdateInstructionKind: 2

        var serializedTx = serializedBlockItemKind.Concat(serializedSignatures).Concat(serializedTransaction).ToArray(); 
        var txHash = SHA256.Create().ComputeHash(serializedTx);

        var serializedVersion = new byte[] { 0 };
        var serializedForSubmission = serializedVersion.Concat(serializedTx).ToArray();

        await _client.SendTransactionAsync(serializedForSubmission);

        return Convert.ToHexString(txHash).ToLowerInvariant();
    }

    private byte[] CreateSerializedHeader(AccountAddress sender, int signatureCount, int payloadSize, Amount baseEnergyCost, Nonce accountNonce)
    {
        var energyCost = CalculateEnergyCost(signatureCount, payloadSize, baseEnergyCost);
        ulong expiry = (ulong)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds(); 

        var result = new byte[60];
        var span = new Span<byte>(result);
        sender.AsBytes.CopyTo(span.Slice(0, 32));
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(32, 8), accountNonce.AsUInt64);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(40, 8), energyCost.MicroCcdValue);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(48, 4), (UInt32)payloadSize+1);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(52, 8), expiry);
        return result;
    }

    private Amount CalculateEnergyCost(int signatureCount, int payloadSize, Amount transactionSpecificCost)
    {
        const int ConstantA = 100;
        const int ConstantB = 1;
        
        // Account address (32 bytes), nonce (8 bytes), energy (8 bytes), payload size (4 bytes), expiry (8 bytes);
        const int AccountTransactionHeaderSize = 32 + 8 + 8 + 4 + 8;

        var result = transactionSpecificCost +
                     Amount.FromMicroCcd(ConstantA * signatureCount +
                                         ConstantB * (AccountTransactionHeaderSize + payloadSize + 1));
        return result;
    }
}