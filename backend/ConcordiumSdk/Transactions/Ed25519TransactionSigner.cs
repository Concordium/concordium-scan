using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using ConcordiumSdk.Types;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using NSec.Cryptography;

namespace ConcordiumSdk.Transactions;

public class Ed25519TransactionSigner : ITransactionSigner
{
    private readonly byte[] _privateKey;

    public Ed25519TransactionSigner(byte[] privateKey)
    {
        _privateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
    }

    public static Ed25519TransactionSigner CreateFromConcordiumWalletExportFile(
        string exportedWalletFilePath, string password, AccountAddress signerAccount)
    {
        var encrypted = GetEncryptedWallet(exportedWalletFilePath);

        var key = KeyDerivation.Pbkdf2(
            password: password,
            salt: Convert.FromBase64String(encrypted.Metadata.Salt),
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: encrypted.Metadata.Iterations,
            numBytesRequested: 32);

        using var myAes = new AesManaged
        {
            Mode = CipherMode.CBC,
            IV = Convert.FromBase64String(encrypted.Metadata.InitializationVector),
            Key = key,
            Padding = PaddingMode.PKCS7
        };
        using var decryptor = myAes.CreateDecryptor();

        using var ms = new MemoryStream(Convert.FromBase64String(encrypted.CipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var jsonDocument = JsonDocument.Parse(cs);
        var result = jsonDocument.RootElement
            .GetProperty("value")
            .GetProperty("identities").EnumerateArray().SelectMany(identity => identity
                .GetProperty("accounts").EnumerateArray().Where(account => account.GetProperty("address").GetString() == signerAccount.AsString)
                .Select(account => account.GetProperty("accountKeys"))).ToArray();

        if (result.Length == 0)
            throw new InvalidOperationException("Sender account not found in decrypted wallet file.");
        if (result.Length > 1)
            throw new InvalidOperationException("Sender account found in multiple identities in decrypted wallet file.");

        var accountKeys = result.Single();
        var signKeyString = accountKeys.GetProperty("keys").GetProperty("0").GetProperty("keys").GetProperty("0").GetProperty("signKey").GetString();
        var signKeyBytes = Convert.FromHexString(signKeyString);
        return new Ed25519TransactionSigner(signKeyBytes);
    }

    private static EncryptedWallet GetEncryptedWallet(string exportedWalletFilePath)
    {
        var encryptedWallet = File.ReadAllText(exportedWalletFilePath);

        var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true};
        var result = JsonSerializer.Deserialize<EncryptedWallet>(encryptedWallet, options);
        if (result == null) throw new InvalidOperationException("Deserialization returned null which is unexpected.");
        return result;
    }

    public byte[] Sign(byte[] bytes)
    {
        var algorithm = SignatureAlgorithm.Ed25519;
        using var key = Key.Import(algorithm,_privateKey, KeyBlobFormat.RawPrivateKey);
        return algorithm.Sign(key, bytes);
    }

    private class EncryptedWallet
    {
        public EncryptedWallet(string cipherText, EncryptedWalletMetadata metadata)
        {
            CipherText = cipherText ?? throw new ArgumentNullException(nameof(cipherText));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public string CipherText { get; private set; }
        public EncryptedWalletMetadata Metadata { get; private set; }
    }

    private class EncryptedWalletMetadata
    {
        public EncryptedWalletMetadata(string initializationVector, int iterations, string salt)
        {
            InitializationVector = initializationVector ?? throw new ArgumentNullException(nameof(initializationVector));
            Iterations = iterations;
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
        }

        public string InitializationVector { get; }
        public int Iterations { get; }
        public string Salt { get; }
    }
}