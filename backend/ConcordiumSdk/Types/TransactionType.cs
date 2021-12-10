
namespace ConcordiumSdk.Types;

public abstract class TransactionType
{
    public BlockItemKind Kind { get; }
    public object Type { get; }
    protected TransactionType(BlockItemKind kind, object type)
    {
        Kind = kind;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    public static TransactionType<AccountTransactionType> Get(AccountTransactionType type)
    {
        return new TransactionType<AccountTransactionType>(BlockItemKind.AccountTransactionKind, type);
    }
    
    public static TransactionType<CredentialDeploymentTransactionType> Get(CredentialDeploymentTransactionType type)
    {
        return new TransactionType<CredentialDeploymentTransactionType>(BlockItemKind.CredentialDeploymentKind, type);
    }
    
    public static TransactionType<UpdateTransactionType> Get(UpdateTransactionType type)
    {
        return new TransactionType<UpdateTransactionType>(BlockItemKind.UpdateInstructionKind, type);
    }
}

public sealed class TransactionType<T> : TransactionType where T : Enum
{
    public new T Type { get; }

    internal TransactionType(BlockItemKind kind, T type) : base(kind, type)
    {
        Type = type;
    }

    public override string ToString()
    {
        return $"{Kind}.{Type}";
    }

    private bool Equals(TransactionType<T> other)
    {
        return Type.Equals(other.Type)
               && Kind.Equals(other.Kind);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is TransactionType<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(Type);
    }

    public static bool operator ==(TransactionType<T>? left, TransactionType<T>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TransactionType<T>? left, TransactionType<T>? right)
    {
        return !Equals(left, right);
    }
}