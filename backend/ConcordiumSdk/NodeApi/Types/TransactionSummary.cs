using System.Linq;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class TransactionSummary
{
    public TransactionSummary(AccountAddress? sender, TransactionHash hash, CcdAmount cost, int energyCost, TransactionType type, TransactionResult result, int index)
    {
        Sender = sender;
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        Cost = cost;
        EnergyCost = energyCost;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        Index = index;
    }

    public AccountAddress? Sender { get;  }
    public TransactionHash Hash { get; } 
    public CcdAmount Cost { get; }  
    public int EnergyCost { get; }  
    public TransactionType Type { get; }
    public TransactionResult Result { get; }
    
    /// <summary>
    /// The index of the transaction in the block (0 based).
    /// </summary>
    public int Index { get; }

    public IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        if (Sender != null && Cost > CcdAmount.Zero)
            yield return new AccountBalanceUpdate(Sender, -1 * (long)Cost.MicroCcdValue, BalanceUpdateType.TransactionFee);

        if (Result is TransactionSuccessResult success)
        {
            foreach (var x in success.Events.OfType<AmountAddedByDecryption>())
                yield return new AccountBalanceUpdate(x.Account, (long)x.Amount.MicroCcdValue, BalanceUpdateType.AmountDecrypted);
            
            foreach (var x in success.Events.OfType<EncryptedSelfAmountAdded>())
                yield return new AccountBalanceUpdate(x.Account, -1 * (long)x.Amount.MicroCcdValue, BalanceUpdateType.AmountEncrypted);

            foreach (var x in success.Events.OfType<Transferred>())
            {
                if (x.From is AccountAddress fromAccountAddress)
                    yield return new AccountBalanceUpdate(fromAccountAddress, -1 * (long)x.Amount.MicroCcdValue, BalanceUpdateType.TransferOut);
                if (x.To is AccountAddress toAccountAddress)
                    yield return new AccountBalanceUpdate(toAccountAddress, (long)x.Amount.MicroCcdValue, BalanceUpdateType.TransferIn);
            }

            foreach (var x in success.Events.OfType<TransferredWithSchedule>())
            {
                var totalAmount = x.Amount.Sum(amount => (long)amount.Amount.MicroCcdValue);
                yield return new AccountBalanceUpdate(x.From, -1 * totalAmount, BalanceUpdateType.TransferOut);
                yield return new AccountBalanceUpdate(x.To, totalAmount, BalanceUpdateType.TransferIn);
            }

            foreach (var x in success.Events.OfType<ContractInitialized>())
                yield return new AccountBalanceUpdate(Sender!, -1 * (long)x.Amount.MicroCcdValue, BalanceUpdateType.TransferOut);

            foreach (var x in success.Events.OfType<Updated>())
            {
                if (x.Instigator is AccountAddress accountAddress)
                    yield return new AccountBalanceUpdate(accountAddress, -1 * (long)x.Amount.MicroCcdValue, BalanceUpdateType.TransferOut);
            }
        }
    }
}
