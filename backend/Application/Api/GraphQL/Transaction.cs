using System.Text.Json;
using ConcordiumSdk.Types;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL;

public class Transaction
{
    private int _transactionType;
    private int? _transactionSubType;
    private Lazy<TransactionTypeUnion> _transactionTypeUnion;
    private JsonElement? _successEvents;
    
    public Transaction()
    {
        _transactionTypeUnion = new Lazy<TransactionTypeUnion>(CreateTransactionTypeUnion);
    }

    private TransactionTypeUnion CreateTransactionTypeUnion()
    {
        switch ((BlockItemKind)_transactionType)
        {
            case BlockItemKind.AccountTransactionKind:
                return new AccountTransaction { AccountTransactionType = _transactionSubType.HasValue ? (AccountTransactionType)_transactionSubType.Value : null };
            case BlockItemKind.CredentialDeploymentKind:
                return new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = _transactionSubType.HasValue ? (CredentialDeploymentTransactionType)_transactionSubType.Value : null };
            case BlockItemKind.UpdateInstructionKind:
                return new UpdateTransaction { UpdateTransactionType = _transactionSubType.HasValue ? (UpdateTransactionType)_transactionSubType.Value : null };
            default:
                throw new InvalidOperationException("Unknown block item kind.");
        }
    }

    [ID]
    public long Id { get; set; }
    
    [ID(nameof(Block))]
    public long BlockId { get; set; }
    
    public string BlockHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    public int TransactionIndex { get; set; }
    
    public string TransactionHash { get; set; }
    
    public string? SenderAccountAddress { get; set; }
    
    public long CcdCost { get; set; }
    
    public long EnergyCost { get; set; }
    
    public TransactionTypeUnion TransactionType => _transactionTypeUnion.Value;

    public TransactionResult Result => _successEvents != null ? new Successful(Array.Empty<TransactionResultEvent>()) : new Rejected();
}
