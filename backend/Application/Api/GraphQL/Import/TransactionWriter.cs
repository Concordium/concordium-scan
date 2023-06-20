using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Common.Diagnostics;
using Application.NodeApi;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using AccountCreated = Application.Api.GraphQL.Transactions.AccountCreated;
using AlreadyABaker = Application.Api.GraphQL.Transactions.AlreadyABaker;
using AlreadyADelegator = Application.NodeApi.AlreadyADelegator;
using AmountAddedByDecryption = Application.Api.GraphQL.Transactions.AmountAddedByDecryption;
using AmountTooLarge = Application.Api.GraphQL.Transactions.AmountTooLarge;
using BakerAdded = Application.Api.GraphQL.Transactions.BakerAdded;
using BakerInCooldown = Application.Api.GraphQL.Transactions.BakerInCooldown;
using BakerKeysUpdated = Application.Api.GraphQL.Transactions.BakerKeysUpdated;
using BakerRemoved = Application.Api.GraphQL.Transactions.BakerRemoved;
using BakerSetBakingRewardCommission = Concordium.Sdk.Types.New.BakerSetBakingRewardCommission;
using BakerSetFinalizationRewardCommission = Concordium.Sdk.Types.New.BakerSetFinalizationRewardCommission;
using BakerSetMetadataURL = Concordium.Sdk.Types.New.BakerSetMetadataURL;
using BakerSetOpenStatus = Concordium.Sdk.Types.New.BakerSetOpenStatus;
using BakerSetRestakeEarnings = Application.Api.GraphQL.Transactions.BakerSetRestakeEarnings;
using BakerSetTransactionFeeCommission = Concordium.Sdk.Types.New.BakerSetTransactionFeeCommission;
using BakerStakeDecreased = Application.Api.GraphQL.Transactions.BakerStakeDecreased;
using BakerStakeIncreased = Application.Api.GraphQL.Transactions.BakerStakeIncreased;
using BakingRewardCommissionNotInRange = Application.NodeApi.BakingRewardCommissionNotInRange;
using ContractInitialized = Application.Api.GraphQL.Transactions.ContractInitialized;
using CredentialDeployed = Application.Api.GraphQL.Transactions.CredentialDeployed;
using CredentialHolderDidNotSign = Application.Api.GraphQL.Transactions.CredentialHolderDidNotSign;
using CredentialKeysUpdated = Application.Api.GraphQL.Transactions.CredentialKeysUpdated;
using CredentialsUpdated = Application.Api.GraphQL.Transactions.CredentialsUpdated;
using DataRegistered = Application.Api.GraphQL.Transactions.DataRegistered;
using DelegationAdded = Concordium.Sdk.Types.New.DelegationAdded;
using DelegationRemoved = Concordium.Sdk.Types.New.DelegationRemoved;
using DelegationSetDelegationTarget = Concordium.Sdk.Types.New.DelegationSetDelegationTarget;
using DelegationSetRestakeEarnings = Concordium.Sdk.Types.New.DelegationSetRestakeEarnings;
using DelegationStakeDecreased = Concordium.Sdk.Types.New.DelegationStakeDecreased;
using DelegationStakeIncreased = Concordium.Sdk.Types.New.DelegationStakeIncreased;
using DelegationTargetNotABaker = Application.NodeApi.DelegationTargetNotABaker;
using DelegatorInCooldown = Application.NodeApi.DelegatorInCooldown;
using DuplicateAggregationKey = Application.Api.GraphQL.Transactions.DuplicateAggregationKey;
using DuplicateCredIds = Application.Api.GraphQL.Transactions.DuplicateCredIds;
using EncryptedAmountSelfTransfer = Application.Api.GraphQL.Transactions.EncryptedAmountSelfTransfer;
using EncryptedAmountsRemoved = Application.Api.GraphQL.Transactions.EncryptedAmountsRemoved;
using EncryptedSelfAmountAdded = Application.Api.GraphQL.Transactions.EncryptedSelfAmountAdded;
using FinalizationRewardCommissionNotInRange = Application.NodeApi.FinalizationRewardCommissionNotInRange;
using FirstScheduledReleaseExpired = Application.Api.GraphQL.Transactions.FirstScheduledReleaseExpired;
using InsufficientBalanceForBakerStake = Application.Api.GraphQL.Transactions.InsufficientBalanceForBakerStake;
using InsufficientBalanceForDelegationStake = Application.NodeApi.InsufficientBalanceForDelegationStake;
using InsufficientDelegationStake = Application.NodeApi.InsufficientDelegationStake;
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
using MissingBakerAddParameters = Application.NodeApi.MissingBakerAddParameters;
using MissingDelegationAddParameters = Application.NodeApi.MissingDelegationAddParameters;
using ModuleHashAlreadyExists = Application.Api.GraphQL.Transactions.ModuleHashAlreadyExists;
using ModuleNotWf = Application.Api.GraphQL.Transactions.ModuleNotWf;
using NewEncryptedAmount = Application.Api.GraphQL.Transactions.NewEncryptedAmount;
using NonExistentCredentialId = Application.Api.GraphQL.Transactions.NonExistentCredentialId;
using NonExistentCredIds = Application.Api.GraphQL.Transactions.NonExistentCredIds;
using NonExistentRewardAccount = Application.Api.GraphQL.Transactions.NonExistentRewardAccount;
using NonIncreasingSchedule = Application.Api.GraphQL.Transactions.NonIncreasingSchedule;
using NotABaker = Application.Api.GraphQL.Transactions.NotABaker;
using NotADelegator = Application.NodeApi.NotADelegator;
using NotAllowedMultipleCredentials = Application.Api.GraphQL.Transactions.NotAllowedMultipleCredentials;
using NotAllowedToHandleEncrypted = Application.Api.GraphQL.Transactions.NotAllowedToHandleEncrypted;
using NotAllowedToReceiveEncrypted = Application.Api.GraphQL.Transactions.NotAllowedToReceiveEncrypted;
using OutOfEnergy = Application.Api.GraphQL.Transactions.OutOfEnergy;
using PoolClosed = Application.NodeApi.PoolClosed;
using PoolWouldBecomeOverDelegated = Application.NodeApi.PoolWouldBecomeOverDelegated;
using RejectedInit = Application.Api.GraphQL.Transactions.RejectedInit;
using RejectedReceive = Application.Api.GraphQL.Transactions.RejectedReceive;
using RemoveFirstCredential = Application.Api.GraphQL.Transactions.RemoveFirstCredential;
using RuntimeFailure = Application.Api.GraphQL.Transactions.RuntimeFailure;
using ScheduledSelfTransfer = Application.Api.GraphQL.Transactions.ScheduledSelfTransfer;
using SerializationFailure = Application.Api.GraphQL.Transactions.SerializationFailure;
using StakeOverMaximumThresholdForPool = Application.NodeApi.StakeOverMaximumThresholdForPool;
using StakeUnderMinimumThresholdForBaking = Application.Api.GraphQL.Transactions.StakeUnderMinimumThresholdForBaking;
using TransactionFeeCommissionNotInRange = Application.NodeApi.TransactionFeeCommissionNotInRange;
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

    public async Task<TransactionPair[]> AddTransactions(BlockSummaryBase blockSummary, long blockId, DateTimeOffset blockSlotTime)
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
            TransactionHash = value.Hash.ToString(),
            TransactionType = TransactionTypeUnion.CreateFrom(value.Type),
            SenderAccountAddress = value.Sender != null ? MapAccountAddress(value.Sender.Value) : null,
            CcdCost = value.Cost.Value,
            EnergyCost = Convert.ToUInt64(value.EnergyCost), // TODO: Is energy cost Int or UInt64 in CC?
            RejectReason = MapRejectReason(value.Result as TransactionRejectResult),
        };
    }

    private TransactionRelated<TransactionResultEvent> MapTransactionEvent(Transaction owner, int index,
        Concordium.Sdk.Types.New.TransactionResultEvent value, DateTimeOffset blockSlotTime)
    {
        return new TransactionRelated<TransactionResultEvent>
        {
            TransactionId = owner.Id,
            Index = index,
            Entity = value switch
            {
                Concordium.Sdk.Types.New.Transferred x => new Transferred(x.Amount.Value, MapAddress(x.From), MapAddress(x.To)),
                Concordium.Sdk.Types.New.AccountCreated x => new AccountCreated(MapAccountAddress(x.Contents)),
                Concordium.Sdk.Types.New.CredentialDeployed x => new CredentialDeployed(x.RegId, MapAccountAddress(x.Account)),
                Concordium.Sdk.Types.New.BakerAdded x => new BakerAdded(x.Stake.Value, x.RestakeEarnings, x.BakerId, MapAccountAddress(x.Account), x.SignKey, x.ElectionKey, x.AggregationKey),
                Concordium.Sdk.Types.New.BakerKeysUpdated x => new BakerKeysUpdated(x.BakerId, MapAccountAddress(x.Account), x.SignKey, x.ElectionKey, x.AggregationKey),
                Concordium.Sdk.Types.New.BakerRemoved x => new BakerRemoved(x.BakerId, MapAccountAddress(x.Account)),
                Concordium.Sdk.Types.New.BakerSetRestakeEarnings x => new BakerSetRestakeEarnings(x.BakerId, MapAccountAddress(x.Account), x.RestakeEarnings),
                Concordium.Sdk.Types.New.BakerStakeDecreased x => new BakerStakeDecreased(x.BakerId, MapAccountAddress(x.Account), x.NewStake.Value),
                Concordium.Sdk.Types.New.BakerStakeIncreased x => new BakerStakeIncreased(x.BakerId, MapAccountAddress(x.Account), x.NewStake.Value),
                Concordium.Sdk.Types.New.AmountAddedByDecryption x => new AmountAddedByDecryption(x.Amount.Value, MapAccountAddress(x.Account)),
                Concordium.Sdk.Types.New.EncryptedAmountsRemoved x => new EncryptedAmountsRemoved(MapAccountAddress(x.Account), x.NewAmount, x.InputAmount, x.UpToIndex),
                Concordium.Sdk.Types.New.EncryptedSelfAmountAdded x => new EncryptedSelfAmountAdded(MapAccountAddress(x.Account), x.NewAmount, x.Amount.Value),
                Concordium.Sdk.Types.New.NewEncryptedAmount x => new NewEncryptedAmount(MapAccountAddress(x.Account), x.NewIndex, x.EncryptedAmount),
                Concordium.Sdk.Types.New.CredentialKeysUpdated x => new CredentialKeysUpdated(x.CredId),
                Concordium.Sdk.Types.New.CredentialsUpdated x => new CredentialsUpdated(MapAccountAddress(x.Account), x.NewCredIds, x.RemovedCredIds, x.NewThreshold),
                Concordium.Sdk.Types.New.ContractInitialized x => new ContractInitialized(x.Ref.ToString(), MapContractAddress(x.Address), x.Amount.Value, x.InitName, x.Events.Select(data => data.AsHexString).ToArray()),
                ModuleDeployed x => new ContractModuleDeployed(x.Contents.ToString()),
                Updated x => new ContractUpdated(MapContractAddress(x.Address), MapAddress(x.Instigator), x.Amount.Value, x.Message.AsHexString, x.ReceiveName, x.Events.Select(data => data.AsHexString).ToArray()),
                Interrupted x => new ContractInterrupted(MapContractAddress(x.Address), x.Events.Select(data => data.AsHexString).ToArray()),
                Resumed x => new ContractResumed(MapContractAddress(x.Address), x.Success),
                Upgraded x => new ContractUpgraded(MapContractAddress(x.Address), x.From.ToString(), x.To.ToString()),
                Concordium.Sdk.Types.New.TransferredWithSchedule x => new TransferredWithSchedule(MapAccountAddress(x.From), MapAccountAddress(x.To), x.Amount.Select(amount => new TimestampedAmount(amount.Timestamp, amount.Amount.Value)).ToArray()),
                Concordium.Sdk.Types.New.DataRegistered x => new DataRegistered(x.Data.AsHex),
                Concordium.Sdk.Types.New.TransferMemo x => new TransferMemo(x.Memo.AsHex),
                UpdateEnqueued x => MapChainUpdateEnqueued(x, blockSlotTime),
                BakerSetOpenStatus x => new Transactions.BakerSetOpenStatus(x.BakerId, MapAccountAddress(x.Account), x.OpenStatus.MapToGraphQlEnum()),
                BakerSetMetadataURL x => new Transactions.BakerSetMetadataURL(x.BakerId, MapAccountAddress(x.Account), x.MetadataURL),
                BakerSetTransactionFeeCommission x => new Transactions.BakerSetTransactionFeeCommission(x.BakerId, MapAccountAddress(x.Account), x.TransactionFeeCommission),
                BakerSetBakingRewardCommission x => new Transactions.BakerSetBakingRewardCommission(x.BakerId, MapAccountAddress(x.Account), x.BakingRewardCommission),
                BakerSetFinalizationRewardCommission x => new Transactions.BakerSetFinalizationRewardCommission(x.BakerId, MapAccountAddress(x.Account), x.FinalizationRewardCommission),
                DelegationAdded x => new Transactions.DelegationAdded(x.DelegatorId, MapAccountAddress(x.Account)),
                DelegationRemoved x => new Transactions.DelegationRemoved(x.DelegatorId, MapAccountAddress(x.Account)),
                DelegationStakeIncreased x => new Transactions.DelegationStakeIncreased(x.DelegatorId, MapAccountAddress(x.Account), x.NewStake.Value),
                DelegationStakeDecreased x => new Transactions.DelegationStakeDecreased(x.DelegatorId, MapAccountAddress(x.Account), x.NewStake.Value),
                DelegationSetRestakeEarnings x => new Transactions.DelegationSetRestakeEarnings(x.DelegatorId, MapAccountAddress(x.Account), x.RestakeEarnings),
                DelegationSetDelegationTarget x => new Transactions.DelegationSetDelegationTarget(x.DelegatorId, MapAccountAddress(x.Account), MapDelegationTarget(x.DelegationTarget)),
                _ => throw new NotSupportedException($"Cannot map transaction event '{value.GetType()}'")
            }
        };
    }

    private DelegationTarget MapDelegationTarget(Concordium.Sdk.Types.New.DelegationTarget src)
    {
        return src switch
        {
            Concordium.Sdk.Types.New.PassiveDelegationTarget => new PassiveDelegationTarget(),
            Concordium.Sdk.Types.New.BakerDelegationTarget x => new BakerDelegationTarget((long)x.BakerId),
            _ => throw new NotImplementedException()
        };
    }

    private ChainUpdateEnqueued MapChainUpdateEnqueued(UpdateEnqueued value, DateTimeOffset blockSlotTime)
    {
        var isEffectiveImmediately = value.EffectiveTime.AsLong == 0;
        var effectiveTime = isEffectiveImmediately ? blockSlotTime : value.EffectiveTime.AsDateTimeOffset;
        return new ChainUpdateEnqueued(effectiveTime, isEffectiveImmediately, MapUpdatePayload(value.Payload));
    }

    private static AccountAddress MapAccountAddress(Concordium.Sdk.Types.AccountAddress value)
    {
        return new AccountAddress(value.ToString());
    }

    private ChainUpdatePayload MapUpdatePayload(UpdatePayload value)
    {
        return value switch
        {
            ProtocolUpdatePayload x => new ProtocolChainUpdatePayload(x.Content.Message, x.Content.SpecificationURL, x.Content.SpecificationHash, x.Content.SpecificationAuxiliaryData.AsHexString),
            ElectionDifficultyUpdatePayload x => new ElectionDifficultyChainUpdatePayload(x.ElectionDifficulty),
            EuroPerEnergyUpdatePayload x => new EuroPerEnergyChainUpdatePayload(new ExchangeRate {Denominator = x.Content.Denominator, Numerator = x.Content.Numerator }),
            MicroGtuPerEuroUpdatePayload x => new MicroCcdPerEuroChainUpdatePayload(new ExchangeRate {Denominator = x.Content.Denominator, Numerator = x.Content.Numerator }),
            FoundationAccountUpdatePayload x => new FoundationAccountChainUpdatePayload(new AccountAddress(x.Account.ToString())),
            MintDistributionV0UpdatePayload x => new MintDistributionChainUpdatePayload(x.Content.MintPerSlot, x.Content.BakingReward, x.Content.FinalizationReward),
            TransactionFeeDistributionUpdatePayload x => new TransactionFeeDistributionChainUpdatePayload(x.Content.Baker, x.Content.GasAccount),
            GasRewardsUpdatePayload x => new GasRewardsChainUpdatePayload(x.Content.Baker, x.Content.FinalizationProof, x.Content.AccountCreation, x.Content.ChainUpdate),
            BakerStakeThresholdUpdatePayload x => new BakerStakeThresholdChainUpdatePayload(x.Content.MinimumThresholdForBaking.Value),
            RootUpdatePayload x => new RootKeysChainUpdatePayload(),
            Level1UpdatePayload x => new Level1KeysChainUpdatePayload(),
            AddAnonymityRevokerUpdatePayload x => new AddAnonymityRevokerChainUpdatePayload((int)x.Content.ArIdentity, x.Content.ArDescription.Name, x.Content.ArDescription.Url, x.Content.ArDescription.Description),
            AddIdentityProviderUpdatePayload x => new AddIdentityProviderChainUpdatePayload((int)x.Content.IpIdentity, x.Content.IpDescription.Name, x.Content.IpDescription.Url, x.Content.IpDescription.Description),
            CooldownParametersUpdatePayload x => new CooldownParametersChainUpdatePayload(x.Content.PoolOwnerCooldown, x.Content.DelegatorCooldown),
            PoolParametersUpdatePayload x => MapPoolParametersChainUpdatePayload(x),
            TimeParametersUpdatePayload x => new TimeParametersChainUpdatePayload(x.Content.RewardPeriodLength, x.Content.MintPerPayday),
            MintDistributionV1UpdatePayload x => new MintDistributionV1ChainUpdatePayload(x.Content.BakingReward, x.Content.FinalizationReward),
            _ => throw new NotImplementedException()
        };
    }

    private static PoolParametersChainUpdatePayload MapPoolParametersChainUpdatePayload(PoolParametersUpdatePayload x)
    {
        return new PoolParametersChainUpdatePayload(x.Content.PassiveFinalizationCommission,
            x.Content.PassiveBakingCommission, x.Content.PassiveTransactionCommission,
            MapCommissionRange(x.Content.FinalizationCommissionRange),
            MapCommissionRange(x.Content.BakingCommissionRange),
            MapCommissionRange(x.Content.TransactionCommissionRange),
            x.Content.MinimumEquityCapital.Value, x.Content.CapitalBound,
            new LeverageFactor
            {
                Numerator = x.Content.LeverageBound.Numerator,
                Denominator = x.Content.LeverageBound.Denominator
            });
    }

    private Address MapAddress(IAddress value)
    {
        return value switch
        {
            Concordium.Sdk.Types.AccountAddress x => new AccountAddress(x.ToString()),
            Concordium.Sdk.Types.ContractAddress x => MapContractAddress(x),
            _ => throw new NotSupportedException("Cannot map this address type")
        };
    }

    private static ContractAddress MapContractAddress(Concordium.Sdk.Types.ContractAddress value)
    {
        return new ContractAddress(value.Index, value.SubIndex);
    }

    private static CommissionRange MapCommissionRange(InclusiveRange<decimal> src)
    {
        return new () { Min = src.Min, Max = src.Max};
    }

    private TransactionRejectReason? MapRejectReason(TransactionRejectResult? value)
    {
        if (value == null) return null;

        return value.Reason switch
        {
            NodeApi.ModuleNotWf => new ModuleNotWf(),
            NodeApi.ModuleHashAlreadyExists x => new ModuleHashAlreadyExists(x.Contents.ToString()),
            NodeApi.InvalidAccountReference x => new InvalidAccountReference(MapAccountAddress(x.Contents)),
            NodeApi.InvalidInitMethod x => new InvalidInitMethod(x.ModuleRef.ToString(), x.InitName),
            NodeApi.InvalidReceiveMethod x => new InvalidReceiveMethod(x.ModuleRef.ToString(), x.ReceiveName),
            NodeApi.InvalidModuleReference x => new InvalidModuleReference(x.Contents.ToString()),
            NodeApi.InvalidContractAddress x => new InvalidContractAddress(MapContractAddress(x.Contents)),
            NodeApi.RuntimeFailure => new RuntimeFailure(),
            NodeApi.AmountTooLarge x => new AmountTooLarge(MapAddress(x.Address), x.Amount.Value),
            NodeApi.SerializationFailure => new SerializationFailure(),
            NodeApi.OutOfEnergy => new OutOfEnergy(),
            NodeApi.RejectedInit x => new RejectedInit(x.RejectReason),
            NodeApi.RejectedReceive x => new RejectedReceive(x.RejectReason, MapContractAddress(x.ContractAddress), x.ReceiveName, x.Parameter.AsHexString),
            NodeApi.NonExistentRewardAccount x => new NonExistentRewardAccount(MapAccountAddress(x.Contents)),
            NodeApi.InvalidProof => new InvalidProof(),
            NodeApi.AlreadyABaker x => new AlreadyABaker(x.Contents),
            NodeApi.NotABaker x => new NotABaker(MapAccountAddress(x.Contents)),
            NodeApi.InsufficientBalanceForBakerStake => new InsufficientBalanceForBakerStake(),
            NodeApi.StakeUnderMinimumThresholdForBaking => new StakeUnderMinimumThresholdForBaking(),
            NodeApi.BakerInCooldown => new BakerInCooldown(),
            NodeApi.DuplicateAggregationKey x => new DuplicateAggregationKey(x.Contents),
            NodeApi.NonExistentCredentialId => new NonExistentCredentialId(),
            NodeApi.KeyIndexAlreadyInUse => new KeyIndexAlreadyInUse(),
            NodeApi.InvalidAccountThreshold => new InvalidAccountThreshold(),
            NodeApi.InvalidCredentialKeySignThreshold => new InvalidCredentialKeySignThreshold(),
            NodeApi.InvalidEncryptedAmountTransferProof => new InvalidEncryptedAmountTransferProof(),
            NodeApi.InvalidTransferToPublicProof => new InvalidTransferToPublicProof(),
            NodeApi.EncryptedAmountSelfTransfer x => new EncryptedAmountSelfTransfer(MapAccountAddress(x.Contents)),
            NodeApi.InvalidIndexOnEncryptedTransfer => new InvalidIndexOnEncryptedTransfer(),
            NodeApi.ZeroScheduledAmount => new ZeroScheduledAmount(),
            NodeApi.NonIncreasingSchedule => new NonIncreasingSchedule(),
            NodeApi.FirstScheduledReleaseExpired => new FirstScheduledReleaseExpired(),
            NodeApi.ScheduledSelfTransfer x => new ScheduledSelfTransfer(MapAccountAddress(x.Contents)),
            NodeApi.InvalidCredentials => new InvalidCredentials(),
            NodeApi.DuplicateCredIds x => new DuplicateCredIds(x.Contents),
            NodeApi.NonExistentCredIds x => new NonExistentCredIds(x.Contents),
            NodeApi.RemoveFirstCredential => new RemoveFirstCredential(),
            NodeApi.CredentialHolderDidNotSign => new CredentialHolderDidNotSign(),
            NodeApi.NotAllowedMultipleCredentials => new NotAllowedMultipleCredentials(),
            NodeApi.NotAllowedToReceiveEncrypted => new NotAllowedToReceiveEncrypted(),
            NodeApi.NotAllowedToHandleEncrypted => new NotAllowedToHandleEncrypted(),
            MissingBakerAddParameters => new Transactions.MissingBakerAddParameters(),
            FinalizationRewardCommissionNotInRange => new Transactions.FinalizationRewardCommissionNotInRange(),
            BakingRewardCommissionNotInRange => new Transactions.BakingRewardCommissionNotInRange(),
            TransactionFeeCommissionNotInRange => new Transactions.TransactionFeeCommissionNotInRange(),
            AlreadyADelegator => new Transactions.AlreadyADelegator(),
            InsufficientBalanceForDelegationStake => new Transactions.InsufficientBalanceForDelegationStake(),
            MissingDelegationAddParameters => new Transactions.MissingDelegationAddParameters(),
            InsufficientDelegationStake => new Transactions.InsufficientDelegationStake(),
            DelegatorInCooldown => new Transactions.DelegatorInCooldown(),
            NotADelegator x => new Transactions.NotADelegator(MapAccountAddress(x.Contents)),
            DelegationTargetNotABaker x => new Transactions.DelegationTargetNotABaker(x.Contents),
            StakeOverMaximumThresholdForPool => new Transactions.StakeOverMaximumThresholdForPool(),
            PoolWouldBecomeOverDelegated => new Transactions.PoolWouldBecomeOverDelegated(),
            PoolClosed => new Transactions.PoolClosed(),
            _ => throw new NotImplementedException("Reject reason not mapped!")
        };
    }
}