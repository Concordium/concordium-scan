using NSec.Cryptography;

namespace ConcordiumSdk.Transactions;

public class Ed25519TransactionSigner : ITransactionSigner
{
    private readonly byte[] _privateKey;

    public Ed25519TransactionSigner(byte[] privateKey)
    {
        _privateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
    }

    public Ed25519TransactionSigner(string privateKeyAsHexString)
    {
        if (privateKeyAsHexString == null) throw new ArgumentNullException(nameof(privateKeyAsHexString));
        _privateKey = Convert.FromHexString(privateKeyAsHexString);
    }

    public byte[] Sign(byte[] bytes)
    {
        var algorithm = SignatureAlgorithm.Ed25519;
        using var key = Key.Import(algorithm,_privateKey, KeyBlobFormat.RawPrivateKey);
        return algorithm.Sign(key, bytes);
    }
}