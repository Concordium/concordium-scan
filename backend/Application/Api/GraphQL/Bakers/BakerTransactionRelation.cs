using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using BakerAdded = Concordium.Sdk.Types.BakerAdded;
using BakerKeysUpdated = Concordium.Sdk.Types.BakerKeysUpdated;
using BakerRemoved = Concordium.Sdk.Types.BakerRemoved;
using ContractInitialized = Concordium.Sdk.Types.ContractInitialized;
using CredentialKeysUpdated = Concordium.Sdk.Types.CredentialKeysUpdated;
using CredentialsUpdated = Concordium.Sdk.Types.CredentialsUpdated;
using DataRegistered = Concordium.Sdk.Types.DataRegistered;
using TransferredWithSchedule = Concordium.Sdk.Types.TransferredWithSchedule;

namespace Application.Api.GraphQL.Bakers;

public class BakerTransactionRelation
{
    /// <summary>
    /// Not part of schema. Only here to be able to query relations for specific accounts. 
    /// </summary>
    [GraphQLIgnore]
    public long BakerId { get; set; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; set; }
    
    /// <summary>
    /// Not part of schema. Only here to be able to retrieve the transaction. 
    /// </summary>
    [GraphQLIgnore]
    public long TransactionId { get; set; }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public Transaction GetTransaction([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .Single(tx => tx.Id == TransactionId);
    }
    
    internal static bool TryFrom(TransactionPair transactionPair, out BakerTransactionRelation? relation)
    {
        var bakerIds = GetBakerIds(transactionPair.Source).Distinct().ToArray();
        
        switch (bakerIds.Length)
        {
            case 0:
                relation = null;
                return false;
            case 1:
                relation = new BakerTransactionRelation
                {
                    BakerId = (long)bakerIds.Single().Id.Index,
                    TransactionId = transactionPair.Target.Id
                };
                return true;
            default:
                throw new InvalidOperationException("Did not expect multiple baker id's from one transaction");
        }
    }

    private static IEnumerable<BakerId> GetBakerIds(BlockItemSummary blockItemSummary)
    {
        switch (blockItemSummary.Details)
            {
                case AccountTransactionDetails accountTransactionDetails:
                    switch (accountTransactionDetails.Effects)
                    {
                        case BakerAdded bakerAdded:
                            yield return bakerAdded.KeysEvent.BakerId;
                            break;
                        case BakerConfigured bakerConfigured:
                            foreach (var bakerId in bakerConfigured.GetBakerIds())
                            {
                                yield return bakerId;
                            }
                            break;
                        case BakerKeysUpdated bakerKeysUpdated:
                            yield return bakerKeysUpdated.KeysEvent.BakerId;
                            break;
                        case BakerRemoved bakerRemoved:
                            yield return bakerRemoved.BakerId;
                            break;
                        case BakerRestakeEarningsUpdated bakerRestakeEarningsUpdated:
                            yield return bakerRestakeEarningsUpdated.BakerId;
                            break;
                        case BakerStakeUpdated bakerStakeUpdated:
                            if (bakerStakeUpdated.Data is not null)
                            {
                                yield return bakerStakeUpdated.Data.BakerId;
                            }
                            break;
                        case ContractInitialized:
                        case ContractUpdateIssued:
                        case CredentialKeysUpdated:
                        case CredentialsUpdated:
                        case DataRegistered:
                        case DelegationConfigured:
                        case EncryptedAmountTransferred:
                        case ModuleDeployed:
                        case None:
                        case TransferredToEncrypted:
                        case TransferredToPublic:
                        case TransferredWithSchedule:
                        case AccountTransfer:
                            break;
                    }
                    break;
                case AccountCreationDetails:
                case UpdateDetails:
                    break;
            }
    }
}