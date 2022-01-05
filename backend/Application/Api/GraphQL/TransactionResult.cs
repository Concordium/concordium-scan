using HotChocolate.Types;

namespace Application.Api.GraphQL;

[InterfaceType("TransactionResult")]
public abstract class TransactionResult
{
    protected TransactionResult(bool successful)
    {
        Successful = successful;
    }

    public bool Successful { get; }
}

public class Successful : TransactionResult
{
    public Successful() : base(true){}
}

public class Rejected : TransactionResult
{
    public Rejected() : base(false){}
}
