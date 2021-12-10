using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.Transactions;

public class AccountTransactionService
{
    private readonly INodeClient _client;

    public AccountTransactionService(INodeClient client)
    {
        _client = client;
    }

    public async Task<TransactionHash> SendAccountTransactionAsync(AccountAddress sender, IAccountTransactionPayload transactionPayload, ITransactionSigner signer)
    {
        var nextAccountNonceResponse = await _client.GetNextAccountNonceAsync(sender);
        return await SendAccountTransactionAsync(sender, nextAccountNonceResponse.Nonce, transactionPayload, signer);
    }

    public async Task<TransactionHash> SendAccountTransactionAsync(AccountAddress sender, Nonce nextAccountNonce, IAccountTransactionPayload transactionPayload, ITransactionSigner signer)
    {
        var signatureCount = 1;

        var serializedPayload = transactionPayload.SerializeToBytes();
        
        var energyCost = CalculateEnergyCost(signatureCount, serializedPayload.Length, transactionPayload.GetBaseEnergyCost());
        var serializedHeader = SerializeHeader(sender, serializedPayload.Length, nextAccountNonce, energyCost);

        var serializedTransaction = serializedHeader.Concat(serializedPayload).ToArray();

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
        
        var serializedBlockItemKind = new byte[] { (int)BlockItemKind.AccountTransactionKind }; 

        var serializedTx = serializedBlockItemKind.Concat(serializedSignatures).Concat(serializedTransaction).ToArray(); 

        var serializedVersion = new byte[] { 0 };
        var serializedForSubmission = serializedVersion.Concat(serializedTx).ToArray();

        await _client.SendTransactionAsync(serializedForSubmission);

        var txHash = SHA256.Create().ComputeHash(serializedTx);
        return new TransactionHash(txHash);
    }

    private static byte[] SerializeHeader(AccountAddress sender, int payloadSize, Nonce accountNonce, int energyCost)
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

        var result = new byte[60];
        var span = new Span<byte>(result);
        sender.AsBytes.CopyTo(span.Slice(0, 32));
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(32, 8), accountNonce.AsUInt64);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(40, 8), (ulong)energyCost);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(48, 4), (uint)payloadSize);
        BinaryPrimitives.WriteUInt64BigEndian(span.Slice(52, 8), (ulong)expiry);
        return result;
    }

    private int CalculateEnergyCost(int signatureCount, int payloadSize, int transactionSpecificCost)
    {
        const int ConstantA = 100;
        const int ConstantB = 1;
        
        // Account address (32 bytes), nonce (8 bytes), energy (8 bytes), payload size (4 bytes), expiry (8 bytes);
        const int AccountTransactionHeaderSize = 60;

        var result = transactionSpecificCost +
                     ConstantA * signatureCount +
                     ConstantB * (AccountTransactionHeaderSize + payloadSize);
        return result;
    }
}