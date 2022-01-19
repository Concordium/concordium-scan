using System.Data;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class DataUpdateController
{
    private readonly IDbContextFactory<GraphQlDbContext> _dcContextFactory;

    public DataUpdateController(IDbContextFactory<GraphQlDbContext> dcContextFactory)
    {
        _dcContextFactory = dcContextFactory;
    }

    public async Task BlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary)
    {
        // TODO: Handle updates later - consider also implementing a replay feature to support migrations?

        await using var context = await _dcContextFactory.CreateDbContextAsync();
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var block = MapBlock(blockInfo, blockSummary);
        context.Blocks.Add(block);
        
        await context.SaveChangesAsync(); // assigns block id

        var finalizationRewards = blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault();
        if (finalizationRewards != null)
        {
            var toSave = finalizationRewards.FinalizationRewards
                .Select((x, ix) => MapFinalizationReward(block, ix, x))
                .ToArray();
            await context.FinalizationRewards.AddRangeAsync(toSave);
        }

        var bakingRewards = blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault();
        if (bakingRewards != null)
        {
            var toSave = bakingRewards.BakerRewards
                .Select((x, ix) => MapBakingReward(block, ix, x))
                .ToArray();
            await context.BakingRewards.AddRangeAsync(toSave);
        }

        var finalizationData = blockSummary.FinalizationData;
        if (finalizationData != null)
        {
            var toSave = finalizationData.Finalizers
                .Select((x, ix) => MapFinalizer(block, ix, x))
                .ToArray();
            await context.FinalizationSummaryFinalizers.AddRangeAsync(toSave);
        }

        var transactions = blockSummary.TransactionSummaries
            .Where(x => x.Result is TransactionSuccessResult)
            .Select(x => new { Source = x, Mapped = MapTransaction(block, x)})
            .ToArray();
        await context.Transactions.AddRangeAsync(transactions.Select(x => x.Mapped));
        
        await context.SaveChangesAsync();  // assigns transaction ids

        foreach (var transaction in transactions)
        {
            if (transaction.Source.Result is TransactionSuccessResult successResult)
            {
                var events = successResult.Events
                    .Select((x, ix) => MapTransactionEvent(transaction.Mapped, ix, x))
                    .Where(x => x != null) // Remove once all events have been mapped!
                    .Select(x => x!) // Remove once all events have been mapped!
                    .ToArray();

                await context.TransactionResultEvents.AddRangeAsync(events);
            }
        }

        await context.SaveChangesAsync();

        await tx.CommitAsync();
    }

    private TransactionRelated<TransactionResultEvent>? MapTransactionEvent(Transaction owner, int index, ConcordiumSdk.NodeApi.Types.TransactionResultEvent value)
    {
        try
        {
            return new TransactionRelated<TransactionResultEvent>
            {
                TransactionId = owner.Id,
                Index = index,
                Entity = value switch
                {
                    ConcordiumSdk.NodeApi.Types.Transferred x => new Transferred(x.Amount.MicroCcdValue, MapAddress(x.From), MapAddress(x.To)),
                    ConcordiumSdk.NodeApi.Types.AccountCreated x => new AccountCreated(x.Contents.AsString),
                    ConcordiumSdk.NodeApi.Types.CredentialDeployed x => new CredentialDeployed(x.RegId, x.Account.AsString),
                    ConcordiumSdk.NodeApi.Types.BakerAdded x => new BakerAdded(x.Stake.MicroCcdValue, x.RestakeEarnings, x.BakerId, x.Account.AsString, x.SignKey, x.ElectionKey, x.AggregationKey),
                    ConcordiumSdk.NodeApi.Types.BakerKeysUpdated x => new BakerKeysUpdated(x.BakerId, x.Account.AsString, x.SignKey, x.ElectionKey, x.AggregationKey),
                    ConcordiumSdk.NodeApi.Types.BakerRemoved x => new BakerRemoved(x.BakerId, x.Account.AsString),
                    ConcordiumSdk.NodeApi.Types.BakerSetRestakeEarnings x => new BakerSetRestakeEarnings(x.BakerId, x.Account.AsString, x.RestakeEarnings),
                    ConcordiumSdk.NodeApi.Types.BakerStakeDecreased x => new BakerStakeDecreased(x.BakerId, x.Account.AsString, x.NewStake.MicroCcdValue),
                    ConcordiumSdk.NodeApi.Types.BakerStakeIncreased x => new BakerStakeIncreased(x.BakerId, x.Account.AsString, x.NewStake.MicroCcdValue),
                    ConcordiumSdk.NodeApi.Types.AmountAddedByDecryption x => new AmountAddedByDecryption(x.Amount.MicroCcdValue, x.Account.AsString),
                    ConcordiumSdk.NodeApi.Types.EncryptedAmountsRemoved x => new EncryptedAmountsRemoved(x.Account.AsString, x.NewAmount, x.InputAmount, x.UpToIndex),
                    ConcordiumSdk.NodeApi.Types.EncryptedSelfAmountAdded x => new EncryptedSelfAmountAdded(x.Account.AsString, x.NewAmount, x.Amount.MicroCcdValue),
                    ConcordiumSdk.NodeApi.Types.NewEncryptedAmount x => new NewEncryptedAmount(x.Account.AsString, x.NewIndex, x.EncryptedAmount),
                    ConcordiumSdk.NodeApi.Types.CredentialKeysUpdated x => new CredentialKeysUpdated(x.CredId),
                    ConcordiumSdk.NodeApi.Types.CredentialsUpdated x => new CredentialsUpdated(x.Account.AsString, x.NewCredIds, x.RemovedCredIds, x.NewThreshold),
                    ConcordiumSdk.NodeApi.Types.ContractInitialized x => new ContractInitialized(x.Ref.AsString, MapContractAddress(x.Address), x.Amount.MicroCcdValue, x.InitName, x.Events.Select(data => data.AsHexString).ToArray()),
                    ConcordiumSdk.NodeApi.Types.ModuleDeployed x => new ContractModuleDeployed(x.Contents.AsString),
                    ConcordiumSdk.NodeApi.Types.Updated x => new ContractUpdated(MapContractAddress(x.Address), MapAddress(x.Instigator), x.Amount.MicroCcdValue, x.Message.AsHexString, x.ReceiveName, x.Events.Select(data => data.AsHexString).ToArray()),
                    _ => throw new NotSupportedException($"Cannot map transaction event '{value.GetType()}'")
                }
            };
        }
        catch (NotSupportedException) // Remove once all events have been mapped!
        {
            return null;
        }
    }

    private Address MapAddress(ConcordiumSdk.Types.Address value)
    {
        return value switch
        {
            ConcordiumSdk.Types.AccountAddress x => new AccountAddress(x.AsString),
            ConcordiumSdk.Types.ContractAddress x => MapContractAddress(x),
            _ => throw new NotSupportedException("Cannot map this address type")
        };
    }

    private static ContractAddress MapContractAddress(ConcordiumSdk.Types.ContractAddress value)
    {
        return new ContractAddress(value.Index, value.SubIndex);
    }

    private Block MapBlock(BlockInfo blockInfo, BlockSummary blockSummary)
    {
        var block = new Block
        {
            BlockHash = blockInfo.BlockHash.AsString,
            BlockHeight = blockInfo.BlockHeight,
            BlockSlotTime = blockInfo.BlockSlotTime,
            BakerId = blockInfo.BlockBaker,
            Finalized = blockInfo.Finalized,
            TransactionCount = blockInfo.TransactionCount,
            SpecialEvents = new SpecialEvents
            {
                Mint = MapMint(blockSummary.SpecialEvents.OfType<MintSpecialEvent>().SingleOrDefault()),
                FinalizationRewards = MapFinalizationRewards(blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault()),
                BlockRewards = MapBlockRewards(blockSummary.SpecialEvents.OfType<BlockRewardSpecialEvent>().SingleOrDefault()),
                BakingRewards = MapBakingRewards(blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault()),
            },
            FinalizationSummary = MapFinalizationSummary(blockSummary.FinalizationData)
        };
        return block;
    }

    private static BlockRelated<FinalizationReward> MapFinalizationReward(Block block, int index, AccountAddressAmount value)
    {
        return new BlockRelated<FinalizationReward>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new FinalizationReward
            {
                Address = value.Address.AsString,
                Amount = value.Amount.MicroCcdValue
            }
        };
    }

    private static BlockRelated<BakingReward> MapBakingReward(Block block, int index, AccountAddressAmount value)
    {
        return new BlockRelated<BakingReward>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new BakingReward()
            {
                Address = value.Address.AsString,
                Amount = value.Amount.MicroCcdValue
            }
        };
    }

    private static BlockRelated<FinalizationSummaryParty> MapFinalizer(Block block, int index, ConcordiumSdk.NodeApi.Types.FinalizationSummaryParty value)
    {
        return new BlockRelated<FinalizationSummaryParty>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new FinalizationSummaryParty
            {
                BakerId = value.BakerId,
                Weight = value.Weight,
                Signed = value.Signed
            }
        };
    }

    private Transaction MapTransaction(Block block, TransactionSummary value)
    {
        return new Transaction
        {
            BlockId = block.Id,
            TransactionIndex = value.Index,
            TransactionHash = value.Hash.AsString,
            TransactionType = MapTransactionType(value.Type),
            SenderAccountAddress = value.Sender?.AsString,
            CcdCost = value.Cost.MicroCcdValue,
            EnergyCost = Convert.ToUInt64(value.EnergyCost), // TODO: Is energy cost Int or UInt64 in CC?
        };
    }

    private TransactionTypeUnion MapTransactionType(TransactionType value)
    {
        return value switch
        {
            TransactionType<AccountTransactionType> x => new AccountTransaction { AccountTransactionType = x.Type },
            TransactionType<CredentialDeploymentTransactionType> x => new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = x.Type },
            TransactionType<UpdateTransactionType> x => new UpdateTransaction { UpdateTransactionType = x.Type },
            _ => throw new NotSupportedException($"Cannot map this transaction type")
        };
    }

    private FinalizationSummary? MapFinalizationSummary(FinalizationData? data)
    {
        if (data == null) return null;
        return new FinalizationSummary
        {
            FinalizedBlockHash = data.FinalizationBlockPointer.AsString,
            FinalizationIndex = data.FinalizationIndex,
            FinalizationDelay = data.FinalizationDelay,
        };
    }

    private BakingRewards? MapBakingRewards(BakingRewardsSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new BakingRewards
        {
            Remainder = rewards.Remainder.MicroCcdValue,
        };
    }

    private BlockRewards? MapBlockRewards(BlockRewardSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new BlockRewards
        {
            TransactionFees = rewards.TransactionFees.MicroCcdValue,
            OldGasAccount = rewards.OldGasAccount.MicroCcdValue,
            NewGasAccount = rewards.NewGasAccount.MicroCcdValue,
            BakerReward = rewards.BakerReward.MicroCcdValue,
            FoundationCharge = rewards.FoundationCharge.MicroCcdValue,
            BakerAccountAddress = rewards.Baker.AsString,
            FoundationAccountAddress = rewards.FoundationAccount.AsString
        };
    }

    private FinalizationRewards? MapFinalizationRewards(FinalizationRewardsSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new FinalizationRewards
        {
            Remainder = rewards.Remainder.MicroCcdValue,
        };
    }

    private Mint? MapMint(MintSpecialEvent? mint)
    {
        if (mint == null) return null;

        return new Mint
        {
            BakingReward = mint.MintBakingReward.MicroCcdValue,
            FinalizationReward = mint.MintFinalizationReward.MicroCcdValue,
            PlatformDevelopmentCharge = mint.MintPlatformDevelopmentCharge.MicroCcdValue,
            FoundationAccount = mint.FoundationAccount.AsString
        };
    }
}