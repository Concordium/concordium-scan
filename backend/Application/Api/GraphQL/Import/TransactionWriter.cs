using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class TransactionWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public TransactionWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<TransactionPair[]> AddTransactions(BlockSummary blockSummary, long blockId, DateTimeOffset blockSlotTime)
    {
        if (blockSummary.TransactionSummaries.Length == 0) return Array.Empty<TransactionPair>();
        
        using var counter = _metrics.MeasureDuration(nameof(TransactionWriter), nameof(AddTransactions));

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var transactions = blockSummary.TransactionSummaries
            .Select(x => new TransactionPair(x, MapTransaction(x, blockId)))
            .ToArray();
        context.Transactions.AddRange(transactions.Select(x => x.Target));
        await context.SaveChangesAsync(); // assigns transaction ids

        foreach (var transaction in transactions)
        {
            if (transaction.Source.Result is TransactionSuccessResult successResult)
            {
                var events = successResult.Events
                    .Select((x, ix) => MapTransactionEvent(transaction.Target, ix, x, blockSlotTime))
                    .ToArray();

                context.TransactionResultEvents.AddRange(events);
            }
        }
        await context.SaveChangesAsync();
        return transactions;
    }

    private Transaction MapTransaction(TransactionSummary value, long blockId)
    {
        return new Transaction
        {
            BlockId = blockId,
            TransactionIndex = value.Index,
            TransactionHash = value.Hash.AsString,
            TransactionType = TransactionTypeUnion.CreateFrom(value.Type),
            SenderAccountAddress = value.Sender != null ? MapAccountAddress(value.Sender) : null,
            CcdCost = value.Cost.MicroCcdValue,
            EnergyCost = Convert.ToUInt64(value.EnergyCost), // TODO: Is energy cost Int or UInt64 in CC?
            RejectReason = MapRejectReason(value.Result as TransactionRejectResult),
        };
    }

    private TransactionRelated<TransactionResultEvent> MapTransactionEvent(Transaction owner, int index,
        ConcordiumSdk.NodeApi.Types.TransactionResultEvent value, DateTimeOffset blockSlotTime)
    {
        return new TransactionRelated<TransactionResultEvent>
        {
            TransactionId = owner.Id,
            Index = index,
            Entity = value switch
            {
                ConcordiumSdk.NodeApi.Types.Transferred x => new Transferred(x.Amount.MicroCcdValue, MapAddress(x.From), MapAddress(x.To)),
                ConcordiumSdk.NodeApi.Types.AccountCreated x => new AccountCreated(MapAccountAddress(x.Contents)),
                ConcordiumSdk.NodeApi.Types.CredentialDeployed x => new CredentialDeployed(x.RegId, MapAccountAddress(x.Account)),
                ConcordiumSdk.NodeApi.Types.BakerAdded x => new BakerAdded(x.Stake.MicroCcdValue, x.RestakeEarnings, x.BakerId, MapAccountAddress(x.Account), x.SignKey, x.ElectionKey, x.AggregationKey),
                ConcordiumSdk.NodeApi.Types.BakerKeysUpdated x => new BakerKeysUpdated(x.BakerId, MapAccountAddress(x.Account), x.SignKey, x.ElectionKey, x.AggregationKey),
                ConcordiumSdk.NodeApi.Types.BakerRemoved x => new BakerRemoved(x.BakerId, MapAccountAddress(x.Account)),
                ConcordiumSdk.NodeApi.Types.BakerSetRestakeEarnings x => new BakerSetRestakeEarnings(x.BakerId, MapAccountAddress(x.Account), x.RestakeEarnings),
                ConcordiumSdk.NodeApi.Types.BakerStakeDecreased x => new BakerStakeDecreased(x.BakerId, MapAccountAddress(x.Account), x.NewStake.MicroCcdValue),
                ConcordiumSdk.NodeApi.Types.BakerStakeIncreased x => new BakerStakeIncreased(x.BakerId, MapAccountAddress(x.Account), x.NewStake.MicroCcdValue),
                ConcordiumSdk.NodeApi.Types.AmountAddedByDecryption x => new AmountAddedByDecryption(x.Amount.MicroCcdValue, MapAccountAddress(x.Account)),
                ConcordiumSdk.NodeApi.Types.EncryptedAmountsRemoved x => new EncryptedAmountsRemoved(MapAccountAddress(x.Account), x.NewAmount, x.InputAmount, x.UpToIndex),
                ConcordiumSdk.NodeApi.Types.EncryptedSelfAmountAdded x => new EncryptedSelfAmountAdded(MapAccountAddress(x.Account), x.NewAmount, x.Amount.MicroCcdValue),
                ConcordiumSdk.NodeApi.Types.NewEncryptedAmount x => new NewEncryptedAmount(MapAccountAddress(x.Account), x.NewIndex, x.EncryptedAmount),
                ConcordiumSdk.NodeApi.Types.CredentialKeysUpdated x => new CredentialKeysUpdated(x.CredId),
                ConcordiumSdk.NodeApi.Types.CredentialsUpdated x => new CredentialsUpdated(MapAccountAddress(x.Account), x.NewCredIds, x.RemovedCredIds, x.NewThreshold),
                ConcordiumSdk.NodeApi.Types.ContractInitialized x => new ContractInitialized(x.Ref.AsString, MapContractAddress(x.Address), x.Amount.MicroCcdValue, x.InitName, x.Events.Select(data => data.AsHexString).ToArray()),
                ConcordiumSdk.NodeApi.Types.ModuleDeployed x => new ContractModuleDeployed(x.Contents.AsString),
                ConcordiumSdk.NodeApi.Types.Updated x => new ContractUpdated(MapContractAddress(x.Address), MapAddress(x.Instigator), x.Amount.MicroCcdValue, x.Message.AsHexString, x.ReceiveName, x.Events.Select(data => data.AsHexString).ToArray()),
                ConcordiumSdk.NodeApi.Types.TransferredWithSchedule x => new TransferredWithSchedule(MapAccountAddress(x.From), MapAccountAddress(x.To), x.Amount.Select(amount => new TimestampedAmount(amount.Timestamp, amount.Amount.MicroCcdValue)).ToArray()),
                ConcordiumSdk.NodeApi.Types.DataRegistered x => new DataRegistered(x.Data.AsHex),
                ConcordiumSdk.NodeApi.Types.TransferMemo x => new TransferMemo(x.Memo.AsHex),
                ConcordiumSdk.NodeApi.Types.UpdateEnqueued x => MapChainUpdateEnqueued(x, blockSlotTime),
                _ => throw new NotSupportedException($"Cannot map transaction event '{value.GetType()}'")
            }
        };
    }

    private ChainUpdateEnqueued MapChainUpdateEnqueued(UpdateEnqueued value, DateTimeOffset blockSlotTime)
    {
        var isEffectiveImmediately = value.EffectiveTime.AsLong > 0;
        var effectiveTime = isEffectiveImmediately ?  value.EffectiveTime.AsDateTimeOffset : blockSlotTime;
        return new ChainUpdateEnqueued(effectiveTime, isEffectiveImmediately, MapUpdatePayload(value.Payload));
    }

    private static AccountAddress MapAccountAddress(ConcordiumSdk.Types.AccountAddress value)
    {
        return new AccountAddress(value.AsString);
    }

    private ChainUpdatePayload MapUpdatePayload(UpdatePayload value)
    {
        return value switch
        {
            ProtocolUpdatePayload x => new ProtocolChainUpdatePayload(x.Content.Message, x.Content.SpecificationURL, x.Content.SpecificationHash, x.Content.SpecificationAuxiliaryData.AsHexString),
            ElectionDifficultyUpdatePayload x => new ElectionDifficultyChainUpdatePayload(x.ElectionDifficulty),
            EuroPerEnergyUpdatePayload x => new EuroPerEnergyChainUpdatePayload(new ExchangeRate {Denominator = x.Content.Denominator, Numerator = x.Content.Numerator }),
            MicroGtuPerEuroUpdatePayload x => new MicroCcdPerEuroChainUpdatePayload(new ExchangeRate {Denominator = x.Content.Denominator, Numerator = x.Content.Numerator }),
            FoundationAccountUpdatePayload x => new FoundationAccountChainUpdatePayload(new AccountAddress(x.Account.AsString)),
            MintDistributionUpdatePayload x => new MintDistributionChainUpdatePayload(x.Content.MintPerSlot, x.Content.BakingReward, x.Content.FinalizationReward),
            TransactionFeeDistributionUpdatePayload x => new TransactionFeeDistributionChainUpdatePayload(x.Content.Baker, x.Content.GasAccount),
            GasRewardsUpdatePayload x => new GasRewardsChainUpdatePayload(x.Content.Baker, x.Content.FinalizationProof, x.Content.AccountCreation, x.Content.ChainUpdate),
            BakerStakeThresholdUpdatePayload x => new BakerStakeThresholdChainUpdatePayload(x.Amount.MicroCcdValue),
            RootUpdatePayload x => new RootKeysChainUpdatePayload(),
            Level1UpdatePayload x => new Level1KeysChainUpdatePayload(),
            AddAnonymityRevokerUpdatePayload x => new AddAnonymityRevokerChainUpdatePayload((int)x.Content.ArIdentity, x.Content.ArDescription.Name, x.Content.ArDescription.Url, x.Content.ArDescription.Description),
            AddIdentityProviderUpdatePayload x => new AddIdentityProviderChainUpdatePayload((int)x.Content.IpIdentity, x.Content.IpDescription.Name, x.Content.IpDescription.Url, x.Content.IpDescription.Description),
            _ => throw new NotImplementedException()
        };
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

    private TransactionRejectReason? MapRejectReason(TransactionRejectResult? value)
    {
        if (value == null) return null;

        return value.Reason switch
        {
            ConcordiumSdk.NodeApi.Types.ModuleNotWf => new ModuleNotWf(),
            ConcordiumSdk.NodeApi.Types.ModuleHashAlreadyExists x => new ModuleHashAlreadyExists(x.Contents.AsString),
            ConcordiumSdk.NodeApi.Types.InvalidAccountReference x => new InvalidAccountReference(MapAccountAddress(x.Contents)),
            ConcordiumSdk.NodeApi.Types.InvalidInitMethod x => new InvalidInitMethod(x.ModuleRef.AsString, x.InitName),
            ConcordiumSdk.NodeApi.Types.InvalidReceiveMethod x => new InvalidReceiveMethod(x.ModuleRef.AsString, x.ReceiveName),
            ConcordiumSdk.NodeApi.Types.InvalidModuleReference x => new InvalidModuleReference(x.Contents.AsString),
            ConcordiumSdk.NodeApi.Types.InvalidContractAddress x => new InvalidContractAddress(MapContractAddress(x.Contents)),
            ConcordiumSdk.NodeApi.Types.RuntimeFailure => new RuntimeFailure(),
            ConcordiumSdk.NodeApi.Types.AmountTooLarge x => new AmountTooLarge(MapAddress(x.Address), x.Amount.MicroCcdValue),
            ConcordiumSdk.NodeApi.Types.SerializationFailure => new SerializationFailure(),
            ConcordiumSdk.NodeApi.Types.OutOfEnergy => new OutOfEnergy(),
            ConcordiumSdk.NodeApi.Types.RejectedInit x => new RejectedInit(x.RejectReason),
            ConcordiumSdk.NodeApi.Types.RejectedReceive x => new RejectedReceive(x.RejectReason, MapContractAddress(x.ContractAddress), x.ReceiveName, x.Parameter.AsHexString),
            ConcordiumSdk.NodeApi.Types.NonExistentRewardAccount x => new NonExistentRewardAccount(MapAccountAddress(x.Contents)),
            ConcordiumSdk.NodeApi.Types.InvalidProof => new InvalidProof(),
            ConcordiumSdk.NodeApi.Types.AlreadyABaker x => new AlreadyABaker(x.Contents),
            ConcordiumSdk.NodeApi.Types.NotABaker x => new NotABaker(MapAccountAddress(x.Contents)),
            ConcordiumSdk.NodeApi.Types.InsufficientBalanceForBakerStake => new InsufficientBalanceForBakerStake(),
            ConcordiumSdk.NodeApi.Types.StakeUnderMinimumThresholdForBaking => new StakeUnderMinimumThresholdForBaking(),
            ConcordiumSdk.NodeApi.Types.BakerInCooldown => new BakerInCooldown(),
            ConcordiumSdk.NodeApi.Types.DuplicateAggregationKey x => new DuplicateAggregationKey(x.Contents),
            ConcordiumSdk.NodeApi.Types.NonExistentCredentialId => new NonExistentCredentialId(),
            ConcordiumSdk.NodeApi.Types.KeyIndexAlreadyInUse => new KeyIndexAlreadyInUse(),
            ConcordiumSdk.NodeApi.Types.InvalidAccountThreshold => new InvalidAccountThreshold(),
            ConcordiumSdk.NodeApi.Types.InvalidCredentialKeySignThreshold => new InvalidCredentialKeySignThreshold(),
            ConcordiumSdk.NodeApi.Types.InvalidEncryptedAmountTransferProof => new InvalidEncryptedAmountTransferProof(),
            ConcordiumSdk.NodeApi.Types.InvalidTransferToPublicProof => new InvalidTransferToPublicProof(),
            ConcordiumSdk.NodeApi.Types.EncryptedAmountSelfTransfer x => new EncryptedAmountSelfTransfer(MapAccountAddress(x.Contents)),
            ConcordiumSdk.NodeApi.Types.InvalidIndexOnEncryptedTransfer => new InvalidIndexOnEncryptedTransfer(),
            ConcordiumSdk.NodeApi.Types.ZeroScheduledAmount => new ZeroScheduledAmount(),
            ConcordiumSdk.NodeApi.Types.NonIncreasingSchedule => new NonIncreasingSchedule(),
            ConcordiumSdk.NodeApi.Types.FirstScheduledReleaseExpired => new FirstScheduledReleaseExpired(),
            ConcordiumSdk.NodeApi.Types.ScheduledSelfTransfer x => new ScheduledSelfTransfer(MapAccountAddress(x.Contents)),
            ConcordiumSdk.NodeApi.Types.InvalidCredentials => new InvalidCredentials(),
            ConcordiumSdk.NodeApi.Types.DuplicateCredIds x => new DuplicateCredIds(x.Contents),
            ConcordiumSdk.NodeApi.Types.NonExistentCredIds x => new NonExistentCredIds(x.Contents),
            ConcordiumSdk.NodeApi.Types.RemoveFirstCredential => new RemoveFirstCredential(),
            ConcordiumSdk.NodeApi.Types.CredentialHolderDidNotSign => new CredentialHolderDidNotSign(),
            ConcordiumSdk.NodeApi.Types.NotAllowedMultipleCredentials => new NotAllowedMultipleCredentials(),
            ConcordiumSdk.NodeApi.Types.NotAllowedToReceiveEncrypted => new NotAllowedToReceiveEncrypted(),
            ConcordiumSdk.NodeApi.Types.NotAllowedToHandleEncrypted => new NotAllowedToHandleEncrypted(),
            _ => throw new NotImplementedException("Reject reason not mapped!")
        };
    }
}