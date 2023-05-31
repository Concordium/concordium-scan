using System.Collections.Immutable;
using Concordium.Sdk.Crypto;
using Concordium.Sdk.Transactions;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Stubs;

public class TransactionSignerStub : ITransactionSigner
{
    public byte GetSignatureCount()
    {
        throw new NotImplementedException();
    }

    AccountTransactionSignature ITransactionSigner.Sign(byte[] data)
    {
        throw new NotImplementedException();
    }

    public ImmutableDictionary<AccountCredentialIndex, ImmutableDictionary<AccountKeyIndex, ISigner>> GetSignerEntries()
    {
        throw new NotImplementedException();
    }

    public byte[] Sign(byte[] bytes)
    {
        return bytes;
    }
}