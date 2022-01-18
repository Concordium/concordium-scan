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

        var block = Map(blockInfo, blockSummary);
        context.Blocks.Add(block);
        await context.SaveChangesAsync();

        var finalizationRewards = blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault();
        if (finalizationRewards != null)
        {
            var toSave = finalizationRewards.FinalizationRewards
                .Select((x, ix) => new BlockRelated<FinalizationReward>
                {
                    BlockId = block.Id,
                    Index = ix,
                    Entity = new FinalizationReward
                    {
                        Address = x.Address.AsString,
                        Amount = x.Amount.MicroCcdValue
                    }
                })
                .ToArray();
            await context.FinalizationRewards.AddRangeAsync(toSave);
        }

        var bakingRewards = blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault();
        if (bakingRewards != null)
        {
            var toSave = bakingRewards.BakerRewards
                .Select((x, ix) => new BlockRelated<BakingReward>
                {
                    BlockId = block.Id,
                    Index = ix,
                    Entity = new BakingReward()
                    {
                        Address = x.Address.AsString,
                        Amount = x.Amount.MicroCcdValue
                    }
                })
                .ToArray();
            await context.BakingRewards.AddRangeAsync(toSave);
        }

        var finalizationData = blockSummary.FinalizationData;
        if (finalizationData != null)
        {
            var toSave = finalizationData.Finalizers
                .Select((x, ix) => new BlockRelated<FinalizationSummaryParty>
                {
                    BlockId = block.Id,
                    Index = ix,
                    Entity = new FinalizationSummaryParty
                    {
                        BakerId = x.BakerId,
                        Weight = x.Weight,
                        Signed = x.Signed
                    }
                })
                .ToArray();
            await context.FinalizationSummaryFinalizers.AddRangeAsync(toSave);
        }

        var transactions = blockSummary.TransactionSummaries
            .Where(x => x.Result is TransactionSuccessResult)
            .Select(x => new Transaction
            {
                BlockId = block.Id,
                TransactionIndex = x.Index,
                TransactionHash = x.Hash.AsString,
                TransactionType = Map(x.Type),
                SenderAccountAddress = x.Sender?.AsString,
                CcdCost = x.Cost.MicroCcdValue,
                EnergyCost = Convert.ToUInt64(x.EnergyCost), // TODO: Is energy cost Int or UInt64 in CC?
            });
        await context.Transactions.AddRangeAsync(transactions);

        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private TransactionTypeUnion Map(TransactionType value)
    {
        return value switch
        {
            TransactionType<AccountTransactionType> x => new AccountTransaction { AccountTransactionType = x.Type },
            TransactionType<CredentialDeploymentTransactionType> x => new CredentialDeploymentTransaction { CredentialDeploymentTransactionType = x.Type },
            TransactionType<UpdateTransactionType> x => new UpdateTransaction { UpdateTransactionType = x.Type },
            _ => throw new NotSupportedException($"Cannot map this transaction type")
        };
    }

    private Block Map(BlockInfo blockInfo, BlockSummary blockSummary)
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
                Mint = Map(blockSummary.SpecialEvents.OfType<MintSpecialEvent>().SingleOrDefault()),
                FinalizationRewards =
                    Map(blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault()),
                BlockRewards = Map(blockSummary.SpecialEvents.OfType<BlockRewardSpecialEvent>().SingleOrDefault()),
                BakingRewards = Map(blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault()),
            },
            FinalizationSummary = Map(blockSummary.FinalizationData)
        };
        return block;
    }

    private FinalizationSummary? Map(FinalizationData? data)
    {
        if (data == null) return null;
        return new FinalizationSummary
        {
            FinalizedBlockHash = data.FinalizationBlockPointer.AsString,
            FinalizationIndex = data.FinalizationIndex,
            FinalizationDelay = data.FinalizationDelay,
        };
    }

    private BakingRewards? Map(BakingRewardsSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new BakingRewards
        {
            Remainder = rewards.Remainder.MicroCcdValue,
        };
    }

    private BlockRewards? Map(BlockRewardSpecialEvent? rewards)
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

    private FinalizationRewards? Map(FinalizationRewardsSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new FinalizationRewards
        {
            Remainder = rewards.Remainder.MicroCcdValue,
        };
    }

    private Mint? Map(MintSpecialEvent? mint)
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