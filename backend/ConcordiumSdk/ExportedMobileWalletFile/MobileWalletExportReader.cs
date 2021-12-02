using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using ConcordiumSdk.Types.JsonConverters;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public static class MobileWalletExportReader
{
    public static MobileWalletExport ReadAndDecrypt(string exportedWalletFilePath, string password)
    {
        var fileContents = File.ReadAllText(exportedWalletFilePath);
        var encrypted =  DeserializeFileContents(fileContents);
        return Decrypt(encrypted, password);
    }

    private static EncryptedMobileWalletExport DeserializeFileContents(string exportedWalletFileContents)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var result = JsonSerializer.Deserialize<EncryptedMobileWalletExport>(exportedWalletFileContents, options);
            if (result == null) throw new InvalidOperationException("Deserialization returned null which is unexpected.");
            return result;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is JsonException) 
        {
            throw new InvalidOperationException("A value was not found for a required property", ex);
        }
    }

    private static MobileWalletExport Decrypt(EncryptedMobileWalletExport e, string password)
    {
        var key = KeyDerivation.Pbkdf2(
            password: password,
            salt: Convert.FromBase64String(e.Metadata.Salt),
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: e.Metadata.Iterations,
            numBytesRequested: 32);

        using var myAes = new AesManaged
        {
            Mode = CipherMode.CBC,
            IV = Convert.FromBase64String(e.Metadata.InitializationVector),
            Key = key,
            Padding = PaddingMode.PKCS7
        };
        using var decryptor = myAes.CreateDecryptor();

        using var ms = new MemoryStream(Convert.FromBase64String(e.CipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        var decryptedText = new StreamReader(cs).ReadToEnd();
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new AccountAddressConverter());
        
        var root = JsonSerializer.Deserialize<DecryptedMobileWalletExportRoot>(decryptedText, options);
        if (root.Type == "concordium-mobile-wallet-data" && root.V == 1)
        {
            var result = JsonSerializer.Deserialize<MobileWalletExport>(root.Value, options);
            return result;
        }
        throw new InvalidOperationException("The current type and version of the decrypted data is not supported.");
    }

    private class EncryptedMobileWalletExport
    {
        public EncryptedMobileWalletExport(string cipherText, EncryptedMobileWalletExportMetadata metadata)
        {
            CipherText = cipherText ?? throw new ArgumentNullException(nameof(cipherText));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public string CipherText { get; }
        public EncryptedMobileWalletExportMetadata Metadata { get; }
    };

    private class EncryptedMobileWalletExportMetadata
    {
        public EncryptedMobileWalletExportMetadata(string initializationVector, int iterations, string salt)
        {
            if (iterations < 1) throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be greater than 0");
            InitializationVector = initializationVector ?? throw new ArgumentNullException(nameof(initializationVector));
            Iterations = iterations;
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
        }

        public string InitializationVector { get; }
        public int Iterations { get; }
        public string Salt { get; }
    }
    
    private record DecryptedMobileWalletExportRoot(string Environment, string Type, JsonElement Value, int V); 
}