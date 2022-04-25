using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;
using AccountCreated = Application.Api.GraphQL.Transactions.AccountCreated;
using AlreadyABaker = Application.Api.GraphQL.Transactions.AlreadyABaker;
using AmountAddedByDecryption = Application.Api.GraphQL.Transactions.AmountAddedByDecryption;
using AmountTooLarge = Application.Api.GraphQL.Transactions.AmountTooLarge;
using BakerAdded = Application.Api.GraphQL.Transactions.BakerAdded;
using BakerInCooldown = Application.Api.GraphQL.Transactions.BakerInCooldown;
using BakerKeysUpdated = Application.Api.GraphQL.Transactions.BakerKeysUpdated;
using BakerRemoved = Application.Api.GraphQL.Transactions.BakerRemoved;
using BakerSetRestakeEarnings = Application.Api.GraphQL.Transactions.BakerSetRestakeEarnings;
using BakerStakeDecreased = Application.Api.GraphQL.Transactions.BakerStakeDecreased;
using BakerStakeIncreased = Application.Api.GraphQL.Transactions.BakerStakeIncreased;
using ContractInitialized = Application.Api.GraphQL.Transactions.ContractInitialized;
using CredentialDeployed = Application.Api.GraphQL.Transactions.CredentialDeployed;
using CredentialHolderDidNotSign = Application.Api.GraphQL.Transactions.CredentialHolderDidNotSign;
using CredentialKeysUpdated = Application.Api.GraphQL.Transactions.CredentialKeysUpdated;
using CredentialsUpdated = Application.Api.GraphQL.Transactions.CredentialsUpdated;
using DataRegistered = Application.Api.GraphQL.Transactions.DataRegistered;
using DuplicateAggregationKey = Application.Api.GraphQL.Transactions.DuplicateAggregationKey;
using DuplicateCredIds = Application.Api.GraphQL.Transactions.DuplicateCredIds;
using EncryptedAmountSelfTransfer = Application.Api.GraphQL.Transactions.EncryptedAmountSelfTransfer;
using EncryptedAmountsRemoved = Application.Api.GraphQL.Transactions.EncryptedAmountsRemoved;
using EncryptedSelfAmountAdded = Application.Api.GraphQL.Transactions.EncryptedSelfAmountAdded;
using FirstScheduledReleaseExpired = Application.Api.GraphQL.Transactions.FirstScheduledReleaseExpired;
using InsufficientBalanceForBakerStake = Application.Api.GraphQL.Transactions.InsufficientBalanceForBakerStake;
using InvalidAccountReference = Application.Api.GraphQL.Transactions.InvalidAccountReference;
using InvalidAccountThreshold = Application.Api.GraphQL.Transactions.InvalidAccountThreshold;
using InvalidContractAddress = Application.Api.GraphQL.Transactions.InvalidContractAddress;
using InvalidCredentialKeySignThreshold = Application.Api.GraphQL.Transactions.InvalidCredentialKeySignThreshold;
using InvalidCredentials = Application.Api.GraphQL.Transactions.InvalidCredentials;
using InvalidEncryptedAmountTransferProof = Application.Api.GraphQL.Transactions.InvalidEncryptedAmountTransferProof;
using InvalidIndexOnEncryptedTransfer = Application.Api.GraphQL.Transactions.InvalidIndexOnEncryptedTransfer;
using InvalidInitMethod = Application.Api.GraphQL.Transactions.InvalidInitMethod;
using InvalidModuleReference = Application.Api.GraphQL.Transactions.InvalidModuleReference;
using InvalidProof = Application.Api.GraphQL.Transactions.InvalidProof;
using InvalidReceiveMethod = Application.Api.GraphQL.Transactions.InvalidReceiveMethod;
using InvalidTransferToPublicProof = Application.Api.GraphQL.Transactions.InvalidTransferToPublicProof;
using KeyIndexAlreadyInUse = Application.Api.GraphQL.Transactions.KeyIndexAlreadyInUse;
using ModuleHashAlreadyExists = Application.Api.GraphQL.Transactions.ModuleHashAlreadyExists;
using ModuleNotWf = Application.Api.GraphQL.Transactions.ModuleNotWf;
using NewEncryptedAmount = Application.Api.GraphQL.Transactions.NewEncryptedAmount;
using NonExistentCredentialId = Application.Api.GraphQL.Transactions.NonExistentCredentialId;
using NonExistentCredIds = Application.Api.GraphQL.Transactions.NonExistentCredIds;
using NonExistentRewardAccount = Application.Api.GraphQL.Transactions.NonExistentRewardAccount;
using NonIncreasingSchedule = Application.Api.GraphQL.Transactions.NonIncreasingSchedule;
using NotABaker = Application.Api.GraphQL.Transactions.NotABaker;
using NotAllowedMultipleCredentials = Application.Api.GraphQL.Transactions.NotAllowedMultipleCredentials;
using NotAllowedToHandleEncrypted = Application.Api.GraphQL.Transactions.NotAllowedToHandleEncrypted;
using NotAllowedToReceiveEncrypted = Application.Api.GraphQL.Transactions.NotAllowedToReceiveEncrypted;
using OutOfEnergy = Application.Api.GraphQL.Transactions.OutOfEnergy;
using RejectedInit = Application.Api.GraphQL.Transactions.RejectedInit;
using RejectedReceive = Application.Api.GraphQL.Transactions.RejectedReceive;
using RemoveFirstCredential = Application.Api.GraphQL.Transactions.RemoveFirstCredential;
using RuntimeFailure = Application.Api.GraphQL.Transactions.RuntimeFailure;
using ScheduledSelfTransfer = Application.Api.GraphQL.Transactions.ScheduledSelfTransfer;
using SerializationFailure = Application.Api.GraphQL.Transactions.SerializationFailure;
using StakeUnderMinimumThresholdForBaking = Application.Api.GraphQL.Transactions.StakeUnderMinimumThresholdForBaking;
using TransactionRejectReason = Application.Api.GraphQL.Transactions.TransactionRejectReason;
using TransactionResultEvent = Application.Api.GraphQL.Transactions.TransactionResultEvent;
using TransferMemo = Application.Api.GraphQL.Transactions.TransferMemo;
using Transferred = Application.Api.GraphQL.Transactions.Transferred;
using TransferredWithSchedule = Application.Api.GraphQL.Transactions.TransferredWithSchedule;
using ZeroScheduledAmount = Application.Api.GraphQL.Transactions.ZeroScheduledAmount;

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
        var isEffectiveImmediately = value.EffectiveTime.AsLong == 0;
        var effectiveTime = isEffectiveImmediately ? blockSlotTime : value.EffectiveTime.AsDateTimeOffset;
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