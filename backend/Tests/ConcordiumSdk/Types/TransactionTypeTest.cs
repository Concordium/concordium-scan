using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Transactions;
using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class TransactionTypeTest
{

    [Fact]
    public void Equality()
    {
        var a = TransactionType.Get(AccountTransactionType.SimpleTransfer);
        var b = TransactionType.Get(AccountTransactionType.SimpleTransfer);
        var other = TransactionType.Get(AccountTransactionType.UpdateCredentials);
        
        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
        Assert.True(a == b);
        Assert.True(b == a);
        Assert.False(a != b);
        Assert.False(b != a);
        
        Assert.False(a.Equals(other));
        Assert.False(other.Equals(a));
        Assert.False(a == other);
        Assert.False(other == a);
        Assert.True(a != other);
        Assert.True(other != a);
    }
    
    [Fact]
    public void TypePropertyOnBaseAndDerived()
    {
        TransactionType<AccountTransactionType> target = TransactionType.Get(AccountTransactionType.AddBaker);
        
        Assert.Equal(BlockItemKind.AccountTransactionKind, target.Kind);
        Assert.Equal(AccountTransactionType.AddBaker, target.Type);

        TransactionType baseType = target;
        Assert.Equal(BlockItemKind.AccountTransactionKind, baseType.Kind);
        Assert.Equal(AccountTransactionType.AddBaker, baseType.Type);
        Assert.Equal(4, (int)baseType.Type);
        Assert.Equal("AddBaker", baseType.Type.ToString());
    }
}