using System.Collections.Generic;
using Application.Api.GraphQL.Extensions;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Stubs;
using AccountAddress = Concordium.Sdk.Types.AccountAddress;
using AlreadyABaker = Concordium.Sdk.Types.AlreadyABaker;
using AlreadyADelegator = Concordium.Sdk.Types.AlreadyADelegator;
using AmountTooLarge = Concordium.Sdk.Types.AmountTooLarge;
using BakerInCooldown = Concordium.Sdk.Types.BakerInCooldown;
using BakingRewardCommissionNotInRange = Concordium.Sdk.Types.BakingRewardCommissionNotInRange;
using BlockEnergyLimitUpdate = Application.Api.GraphQL.Transactions.BlockEnergyLimitUpdate;
using ContractAddress = Concordium.Sdk.Types.ContractAddress;
using CredentialHolderDidNotSign = Concordium.Sdk.Types.CredentialHolderDidNotSign;
using DelegationAdded = Concordium.Sdk.Types.DelegationAdded;
using DelegationRemoved = Concordium.Sdk.Types.DelegationRemoved;
using DelegationSetDelegationTarget = Concordium.Sdk.Types.DelegationSetDelegationTarget;
using DelegationSetRestakeEarnings = Concordium.Sdk.Types.DelegationSetRestakeEarnings;
using DelegationStakeDecreased = Concordium.Sdk.Types.DelegationStakeDecreased;
using DelegationStakeIncreased = Concordium.Sdk.Types.DelegationStakeIncreased;
using DelegationTargetNotABaker = Concordium.Sdk.Types.DelegationTargetNotABaker;
using DelegatorInCooldown = Concordium.Sdk.Types.DelegatorInCooldown;
using DuplicateAggregationKey = Concordium.Sdk.Types.DuplicateAggregationKey;
using DuplicateCredIds = Concordium.Sdk.Types.DuplicateCredIds;
using EncryptedAmountSelfTransfer = Concordium.Sdk.Types.EncryptedAmountSelfTransfer;
using FinalizationCommitteeParametersUpdate = Application.Api.GraphQL.Transactions.FinalizationCommitteeParametersUpdate;
using FinalizationRewardCommissionNotInRange = Concordium.Sdk.Types.FinalizationRewardCommissionNotInRange;
using FirstScheduledReleaseExpired = Concordium.Sdk.Types.FirstScheduledReleaseExpired;
using GasRewardsCpv2Update = Application.Api.GraphQL.Transactions.GasRewardsCpv2Update;
using InsufficientBalanceForBakerStake = Concordium.Sdk.Types.InsufficientBalanceForBakerStake;
using InsufficientBalanceForDelegationStake = Concordium.Sdk.Types.InsufficientBalanceForDelegationStake;
using InsufficientDelegationStake = Concordium.Sdk.Types.InsufficientDelegationStake;
using InvalidAccountReference = Concordium.Sdk.Types.InvalidAccountReference;
using InvalidAccountThreshold = Concordium.Sdk.Types.InvalidAccountThreshold;
using InvalidContractAddress = Concordium.Sdk.Types.InvalidContractAddress;
using InvalidCredentialKeySignThreshold = Concordium.Sdk.Types.InvalidCredentialKeySignThreshold;
using InvalidCredentials = Concordium.Sdk.Types.InvalidCredentials;
using InvalidEncryptedAmountTransferProof = Concordium.Sdk.Types.InvalidEncryptedAmountTransferProof;
using InvalidIndexOnEncryptedTransfer = Concordium.Sdk.Types.InvalidIndexOnEncryptedTransfer;
using InvalidInitMethod = Concordium.Sdk.Types.InvalidInitMethod;
using InvalidModuleReference = Concordium.Sdk.Types.InvalidModuleReference;
using InvalidProof = Concordium.Sdk.Types.InvalidProof;
using InvalidReceiveMethod = Concordium.Sdk.Types.InvalidReceiveMethod;
using InvalidTransferToPublicProof = Concordium.Sdk.Types.InvalidTransferToPublicProof;
using KeyIndexAlreadyInUse = Concordium.Sdk.Types.KeyIndexAlreadyInUse;
using MinBlockTimeUpdate = Application.Api.GraphQL.Transactions.MinBlockTimeUpdate;
using MissingBakerAddParameters = Concordium.Sdk.Types.MissingBakerAddParameters;
using MissingDelegationAddParameters = Application.Api.GraphQL.Transactions.MissingDelegationAddParameters;
using ModuleHashAlreadyExists = Concordium.Sdk.Types.ModuleHashAlreadyExists;
using ModuleNotWf = Concordium.Sdk.Types.ModuleNotWf;
using NonExistentCredentialId = Concordium.Sdk.Types.NonExistentCredentialId;
using NonExistentCredIds = Concordium.Sdk.Types.NonExistentCredIds;
using NonIncreasingSchedule = Concordium.Sdk.Types.NonIncreasingSchedule;
using NotABaker = Concordium.Sdk.Types.NotABaker;
using NotADelegator = Concordium.Sdk.Types.NotADelegator;
using NotAllowedMultipleCredentials = Concordium.Sdk.Types.NotAllowedMultipleCredentials;
using NotAllowedToHandleEncrypted = Concordium.Sdk.Types.NotAllowedToHandleEncrypted;
using NotAllowedToReceiveEncrypted = Concordium.Sdk.Types.NotAllowedToReceiveEncrypted;
using OutOfEnergy = Concordium.Sdk.Types.OutOfEnergy;
using PoolClosed = Concordium.Sdk.Types.PoolClosed;
using PoolWouldBecomeOverDelegated = Concordium.Sdk.Types.PoolWouldBecomeOverDelegated;
using RejectedInit = Concordium.Sdk.Types.RejectedInit;
using RejectedReceive = Concordium.Sdk.Types.RejectedReceive;
using RemoveFirstCredential = Concordium.Sdk.Types.RemoveFirstCredential;
using RuntimeFailure = Concordium.Sdk.Types.RuntimeFailure;
using ScheduledSelfTransfer = Concordium.Sdk.Types.ScheduledSelfTransfer;
using SerializationFailure = Concordium.Sdk.Types.SerializationFailure;
using StakeOverMaximumThresholdForPool = Concordium.Sdk.Types.StakeOverMaximumThresholdForPool;
using StakeUnderMinimumThresholdForBaking = Concordium.Sdk.Types.StakeUnderMinimumThresholdForBaking;
using TimeoutParametersUpdate = Application.Api.GraphQL.Transactions.TimeoutParametersUpdate;
using TransactionFeeCommissionNotInRange = Concordium.Sdk.Types.TransactionFeeCommissionNotInRange;
using TransactionHash = Concordium.Sdk.Types.TransactionHash;
using TransferredWithSchedule = Concordium.Sdk.Types.TransferredWithSchedule;
using ZeroScheduledAmount = Concordium.Sdk.Types.ZeroScheduledAmount;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class TransactionsWriterTest
{
    private readonly TransactionWriter _target;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyBlockSlotTime = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public TransactionsWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new TransactionWriter(_dbContextFactory, new NullMetrics());

        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_transactions");
        connection.Execute("TRUNCATE TABLE graphql_transaction_events");
    }
    
    [Fact]
    public async Task Transactions_BasicInformation_AllValuesNonNull()
    {
        const string sender = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string transactionHash = "42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b";
        const ulong amount = 45872UL;
        const int energyCost = 399;
        var dataRegistered = new Concordium.Sdk.Types.DataRegistered(Array.Empty<byte>());
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(dataRegistered)
            .WithSender(AccountAddress.From(sender))
            .WithCost(CcdAmount.FromMicroCcd(amount))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .WithIndex(0)
            .WithTransactionHash(TransactionHash.From(transactionHash))
            .WithEnergyAmount(new EnergyAmount(energyCost))
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummary}, 133);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();
        transaction.Id.Should().BeGreaterThan(0);
        transaction.BlockId.Should().Be(133);
        transaction.TransactionIndex.Should().Be(0);
        transaction.TransactionHash.Should().Be(transactionHash);
        transaction.SenderAccountAddress!.AsString.Should().Be(sender);
        transaction.CcdCost.Should().Be(amount);
        transaction.EnergyCost.Should().Be(energyCost);
    }

    [Fact]
    public async Task TransactionEvents_TransactionIdAndIndex()
    {
        // Arrange
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;
        const string firstEvent = "05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        const string secondEvent = "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        
        var interrupted = new Interrupted(
            ContractAddress.From(index, subIndex),
            new List<ContractEvent>
            {
                new(Convert.FromHexString(firstEvent)),
                new(Convert.FromHexString(secondEvent))
            });
        var resumed = new Resumed(
            ContractAddress.From(index, subIndex),
            true);
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{interrupted, resumed});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();

        var result = dbContext.TransactionResultEvents.ToArray();
        result.Length.Should().Be(2);
        result[0].TransactionId.Should().Be(transaction.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Should().BeOfType<ContractInterrupted>();
        result[1].TransactionId.Should().Be(transaction.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Should().BeOfType<ContractResumed>();
    }
    
    [Fact]
    public async Task TransactionEvents_Transferred()
    {
        const int contractIndex = 234;
        const int contractSubIndex = 32;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const int ccd = 458382;
        var transferred = new Concordium.Sdk.Types.Transferred(ContractAddress.From(contractIndex, contractSubIndex), CcdAmount.FromMicroCcd(ccd), AccountAddress.From(address));
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{transferred});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.Transferred>();
        result.Amount.Should().Be(ccd);
        result.To.Should().Be(new Application.Api.GraphQL.Accounts.AccountAddress(address));
        result.From.Should().Be(new Application.Api.GraphQL.ContractAddress(contractIndex, contractSubIndex));
    }
    
    [Fact]
    public async Task TransactionEvents_FromAccountCreationNormal_ThenAccountCreatedAndCredentialDeployed()
    {
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var bytes = new byte[42];

        var accountCreated = new AccountCreationDetailsBuilder(CredentialType.Normal)
            .WithAccountAddress(AccountAddress.From(address))
            .WithCredentialRegistrationId(new CredentialRegistrationId(bytes))
            .Build();
        var blockItemSummaryAccountCreated = new BlockItemSummaryBuilder(accountCreated)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummaryAccountCreated});

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();

        var result = dbContext.TransactionResultEvents.ToArray();
        result.Length.Should().Be(2);
        result[0].TransactionId.Should().Be(transaction.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Should().BeOfType<CredentialDeployed>();
        var credentialDeployed = result[0].Entity as CredentialDeployed;
        credentialDeployed!.AccountAddress.AsString.Should().Be(address);
        credentialDeployed!.RegId.Should().Be(Convert.ToHexString(bytes));
        result[1].TransactionId.Should().Be(transaction.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Should().BeOfType<AccountCreated>();
        (result[1].Entity as AccountCreated)!.AccountAddress.AsString.Should().Be(address);
    }
    
    [Fact]
    public async Task TransactionEvents_FromAccountCreationInitial_ThenAccountCreatedAndCredentialDeployed()
    {
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string regId = "b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d";

        var accountCreationDetails = new AccountCreationDetailsBuilder(CredentialType.Initial)
            .WithCredentialRegistrationId(new CredentialRegistrationId(Convert.FromHexString(regId)))
            .WithAccountAddress(AccountAddress.From(address))
            .Build();
        var blockItemSummaryCredentialDeployed = new BlockItemSummaryBuilder(accountCreationDetails)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummaryCredentialDeployed});

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();

        var result = dbContext.TransactionResultEvents.ToArray();
        result.Length.Should().Be(2);
        result[0].TransactionId.Should().Be(transaction.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Should().BeOfType<CredentialDeployed>();
        var credentialDeployed = result[0].Entity as CredentialDeployed;
        credentialDeployed!.AccountAddress.AsString.Should().Be(address);
        credentialDeployed!.RegId.Should().Be(regId);
        result[1].TransactionId.Should().Be(transaction.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Should().BeOfType<AccountCreated>();
        (result[1].Entity as AccountCreated)!.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionEvents_BakerAdded()
    {
        const ulong bakerId = 17UL;
        const ulong amount = 12551UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string signKey = "418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c";
        const string electionKey = "dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88";
        const string aggregationKey = "823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1";
        
        var bakerAdded = new Concordium.Sdk.Types.BakerAdded(
            new BakerKeysEvent(
                new BakerId(new AccountIndex(bakerId)),
                AccountAddress.From(address),
                Convert.FromHexString(signKey),
                Convert.FromHexString(electionKey),
                Convert.FromHexString(aggregationKey)
            ),
            CcdAmount.FromMicroCcd(amount),
            true);
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerAdded)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.BakerAdded>();
        result.StakedAmount.Should().Be(amount);
        result.RestakeEarnings.Should().BeTrue();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.SignKey.Should().Be(signKey);
        result.ElectionKey.Should().Be(electionKey);
        result.AggregationKey.Should().Be(aggregationKey);
    }

    [Fact]
    public async Task TransactionEvents_BakerKeysUpdated()
    {
        const ulong bakerId = 19UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string signKey = "418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c";
        const string electionKey = "dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88";
        const string aggregationKey = "823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1";
        
        var bakerKeysUpdated = new Concordium.Sdk.Types.BakerKeysUpdated(
            new BakerKeysEvent(
                new BakerId(new AccountIndex(bakerId)),
                AccountAddress.From(address),
                Convert.FromHexString(signKey),
                Convert.FromHexString(electionKey),
                Convert.FromHexString(aggregationKey)
                ));
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerKeysUpdated)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.BakerKeysUpdated>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.SignKey.Should().Be(signKey);
        result.ElectionKey.Should().Be(electionKey);
        result.AggregationKey.Should().Be(aggregationKey);
    }

    [Fact]
    public async Task TransactionEvents_BakerRemoved()
    {
        const ulong bakerId = 21UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var bakerRemoved = new Concordium.Sdk.Types.BakerRemoved(new BakerId(new AccountIndex(21)));
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerRemoved)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.BakerRemoved>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetRestakeEarnings()
    {
        const ulong bakerId = 23UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var bakerRestakeEarningsUpdated = new BakerRestakeEarningsUpdated(new BakerId(new AccountIndex(bakerId)), true);
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerRestakeEarningsUpdated)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<BakerSetRestakeEarnings>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.RestakeEarnings.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionEvents_BakerStakeDecreased()
    {
        const ulong bakerId = 23UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong amount = 34786451;
        var bakerConfigured = new BakerConfigured(new List<IBakerEvent>{new BakerStakeDecreasedEvent(new BakerId(new AccountIndex(bakerId)), CcdAmount.FromMicroCcd(amount))});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerConfigured)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<BakerStakeDecreased>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.NewStakedAmount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionEvents_BakerStakeIncreased()
    {
        const ulong bakerId = 23UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong amount = 34786451;
        var bakerConfigured = new BakerConfigured(new List<IBakerEvent>{new BakerStakeIncreasedEvent(new BakerId(new AccountIndex(bakerId)), CcdAmount.FromMicroCcd(amount))});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(bakerConfigured)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<BakerStakeIncreased>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.NewStakedAmount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionEvents_AmountAddedByDecryption()
    {
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong amount = 2362462;
        var transferredToPublic = new TransferredToPublic(
            new EncryptedAmountRemovedEvent(AccountAddress.From(address), Array.Empty<byte>(), Array.Empty<byte>(), 0),
            CcdAmount.FromMicroCcd(amount));
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(transferredToPublic)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        var result = await ReadSingleTransactionEventType<AmountAddedByDecryption>();
        result.Amount.Should().Be(amount);
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionEvents_EncryptedAmountsRemoved()
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string newAmount = "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2";
        const string inputAmount = "acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074";
        const ulong upToIndex = 789UL;
        
        const ulong newIndex = 155UL;
        const string encryptedAmount = "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2";

        var encryptedAmountTransferred = new EncryptedAmountTransferred(
            new EncryptedAmountRemovedEvent(
                AccountAddress.From(address),
                Convert.FromHexString(newAmount),
                Convert.FromHexString(inputAmount),
                upToIndex
            ),
            new NewEncryptedAmountEvent(
                AccountAddress.From(address),
                newIndex,
                Convert.FromHexString(encryptedAmount)
                ), null
        );
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(encryptedAmountTransferred)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext
            .TransactionResultEvents
            .Take(2)
            .ToListAsync();
        var encryptedAmountsRemoveds = result
            .Where(r => r.Entity is EncryptedAmountsRemoved)
            .Select(r => r.Entity as EncryptedAmountsRemoved)
            .Single(r => r is not null);
        encryptedAmountsRemoveds.Should().NotBeNull();
        
        encryptedAmountsRemoveds!.AccountAddress.AsString.Should().Be(address);
        encryptedAmountsRemoveds!.NewEncryptedAmount.Should().Be(newAmount);
        encryptedAmountsRemoveds!.InputAmount.Should().Be(inputAmount);
        encryptedAmountsRemoveds!.UpToIndex.Should().Be(upToIndex);
    }
    
    [Fact]
    public async Task TransactionEvents_NewEncryptedAmount()
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string newAmount = "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2";
        const string inputAmount = "acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074";
        const ulong upToIndex = 789UL;
        
        const ulong newIndex = 155UL;
        const string encryptedAmount = "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2";

        var encryptedAmountTransferred = new EncryptedAmountTransferred(
            new EncryptedAmountRemovedEvent(
                AccountAddress.From(address),
                Convert.FromHexString(newAmount),
                Convert.FromHexString(inputAmount),
                upToIndex
            ),
            new NewEncryptedAmountEvent(
                AccountAddress.From(address),
                newIndex,
                Convert.FromHexString(encryptedAmount)
                ), null
        );
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(encryptedAmountTransferred)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext
            .TransactionResultEvents
            .Take(2)
            .ToListAsync();
        var newEncryptedAmount = result
            .Where(r => r.Entity is NewEncryptedAmount)
            .Select(r => r.Entity as NewEncryptedAmount)
            .Single(r => r is not null);
        newEncryptedAmount.Should().NotBeNull();
        
        newEncryptedAmount!.AccountAddress.AsString.Should().Be(address);
        newEncryptedAmount!.NewIndex.Should().Be(newIndex);
        newEncryptedAmount!.EncryptedAmount.Should().Be(encryptedAmount);
    }

    [Fact]
    public async Task TransactionEvents_EncryptedSelfAmountAdded()
    {
        // Assert
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string newAmount = "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2";
        const int amount = 23446;

        var transferredToEncrypted = new TransferredToEncrypted(new EncryptedSelfAmountAddedEvent(
            AccountAddress.From(address),
            Convert.FromHexString(newAmount),
            CcdAmount.FromMicroCcd(amount)
        ));
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(transferredToEncrypted)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<EncryptedSelfAmountAdded>();
        result.AccountAddress.AsString.Should().Be(address);
        result.NewEncryptedAmount.Should().Be(newAmount);
        result.Amount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionEvents_CredentialKeysUpdated()
    {
        // Arrange
        const string credId = "b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d";
        var credentialKeysUpdated = new Concordium.Sdk.Types.CredentialKeysUpdated(new CredentialRegistrationId(Convert.FromHexString(credId)));
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(credentialKeysUpdated)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.CredentialKeysUpdated>();
        result.CredId.Should().Be(credId);
    }

    [Fact]
    public async Task TransactionEvents_CredentialsUpdated()
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string newCredIds = "b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d";
        const byte newThreshold = 123;
        
        var credentialsUpdated = new Concordium.Sdk.Types.CredentialsUpdated(
            new List<CredentialRegistrationId>{new(Convert.FromHexString(newCredIds))},
            new List<CredentialRegistrationId>(),
            new AccountThreshold(newThreshold));

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(credentialsUpdated)
            .WithSender(AccountAddress.From(address))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.CredentialsUpdated>();
        result.AccountAddress.AsString.Should().Be(address);
        result.NewCredIds.Should().Equal(newCredIds);
        result.RemovedCredIds.Should().BeEmpty();
        result.NewThreshold.Should().Be(newThreshold);
    }

    [Fact]
    public async Task TransactionEvents_ContractInitialized()
    {
        // Arrange
        const string moduleReference = "2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb";
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;
        const ulong amount = 5345462UL;
        const string initName = "init_CIS1-singleNFT";
        var _ = ContractName.TryParse(initName, out var output);
        const string firstEvent = "fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4";
        const string secondEvent = "fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00";

        var contractInitialized = new Concordium.Sdk.Types.ContractInitialized(
            new ContractInitializedEvent(
                ContractVersion.V1,
                new ModuleReference(moduleReference),
                ContractAddress.From(index, subIndex),
                CcdAmount.FromMicroCcd(amount),
                output.ContractName!,
                new List<ContractEvent>
                {
                    new(Convert.FromHexString(firstEvent)),
                    new(Convert.FromHexString(secondEvent))
                }));
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractInitialized)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.ContractInitialized>();
        result.ModuleRef.Should().Be(moduleReference);
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
        result.Amount.Should().Be(amount);
        result.InitName.Should().Be(initName);
        result.EventsAsHex.Should().Equal(firstEvent, secondEvent);
        result.Version.Should().Be(Application.Api.GraphQL.ContractVersion.V1);
    }

    [Fact]
    public async Task TransactionEvents_ContractModuleDeployed()
    {
        // Arrange
        const string moduleReference = "2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb";
        var moduleDeployed = new ModuleDeployed(new ModuleReference(moduleReference));
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(moduleDeployed)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ContractModuleDeployed>();
        result.ModuleRef.Should().Be(moduleReference);
    }

    [Fact]
    public async Task TransactionEvents_ContractUpdated()
    {
        // Arrange
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string parameterHex = "080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        const string name = "inventory.transfer";
        var _ = ReceiveName.TryParse(name, out var output);
        const ulong amount = 15674371UL;
        const string firstEvent = "05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        const string secondEvent = "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";

        var updated = new Updated(
            ContractVersion.V1,
            ContractAddress.From(index, subIndex),
            AccountAddress.From(address),
                CcdAmount.FromMicroCcd(amount),
            new Parameter(Convert.FromHexString(parameterHex)), 
            output.ReceiveName!,
                new List<ContractEvent>
                {
                    new(Convert.FromHexString(firstEvent)),
                    new(Convert.FromHexString(secondEvent))
                });
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{updated});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ContractUpdated>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
        result.Instigator.Should().Be(new Application.Api.GraphQL.Accounts.AccountAddress(address));
        result.Amount.Should().Be(amount);
        result.MessageAsHex.Should().Be(parameterHex);
        result.ReceiveName.Should().Be(name);
        result.EventsAsHex.Should().Equal(firstEvent, secondEvent);
        result.Version.Should().Be(Application.Api.GraphQL.ContractVersion.V1);
    }

    [Fact]
    public async Task TransactionEvents_ContractInterrupted()
    {
        // Arrange
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;
        const string firstEvent = "05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        const string secondEvent = "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        
        var interrupted = new Interrupted(
            ContractAddress.From(index, subIndex),
            new List<ContractEvent>
            {
                new(Convert.FromHexString(firstEvent)),
                new(Convert.FromHexString(secondEvent))
            });
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{interrupted});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ContractInterrupted>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
        result.EventsAsHex.Should().Equal(firstEvent, secondEvent);
    }

    [Fact]
    public async Task TransactionEvents_ContractResumed()
    {
        // Arrange
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;

        var resumed = new Resumed(
            ContractAddress.From(index, subIndex),
            true);
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{resumed});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<ContractResumed>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
        result.Success.Should().BeTrue();
    }


    [Fact]
    public async Task TransactionEvents_ContractUpgraded()
    {
        // Arrange
        const ulong index = 1423UL;
        const ulong subIndex = 1UL;
        const string from = "73ba390d9ce2bb1bf54f124bb00e9dee0d6dc40d6de0f5ba06e1d1f095e4afcc";
        const string to = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        
        const string firstEvent = "05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        const string secondEvent = "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
        
        var upgraded = new Upgraded(
            ContractAddress.From(index, subIndex),
            new ModuleReference(from),
            new ModuleReference(to));
        var contractUpdateIssued = new ContractUpdateIssued(new List<IContractTraceElement>{upgraded});

        var accountTransactionDetails = new AccountTransactionDetailsBuilder(contractUpdateIssued)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<ContractUpgraded>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
        result.From.Should().Be(from);
        result.To.Should().Be(to);
    }

    [Fact]
    public async Task TransactionEvents_TransferredWithSchedule()
    {
        // Arrange        
        const string to = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string from = "3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH";
        var baseTimestamp = new DateTimeOffset(2010, 10, 01, 12, 0, 0, TimeSpan.Zero);
        var tupleOne = (baseTimestamp.AddHours(10), CcdAmount.FromMicroCcd(1000));
        var tupleSecond = (baseTimestamp.AddHours(20), CcdAmount.FromMicroCcd(3333));
        var tupleThird = (baseTimestamp.AddHours(30), CcdAmount.FromMicroCcd(2111));
        
        var valueTuples = new List<(DateTimeOffset, CcdAmount)> { tupleOne, tupleSecond, tupleThird };
        var transferredWithSchedule = new TransferredWithSchedule(AccountAddress.From(to), valueTuples, null);
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(transferredWithSchedule)
            .WithSender(AccountAddress.From(from))
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.TransferredWithSchedule>();
        result.FromAccountAddress.AsString.Should().Be(from);
        result.ToAccountAddress.AsString.Should().Be(to);
        result.AmountsSchedule.Should().Equal(
            new Application.Api.GraphQL.TimestampedAmount(tupleOne.Item1, tupleOne.Item2.Value),
            new Application.Api.GraphQL.TimestampedAmount(tupleSecond.Item1, tupleSecond.Item2.Value),
            new Application.Api.GraphQL.TimestampedAmount(tupleThird.Item1, tupleThird.Item2.Value));
    }
    
    [Fact]
    public async Task TransactionEvents_DataRegistered()
    {
        // Arrange
        const string data = "784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863";

        var dataRegistered = new Concordium.Sdk.Types.DataRegistered(Convert.FromHexString(data));
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(dataRegistered)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DataRegistered>();
        result.DataAsHex.Should().Be(data);
    }

    [Fact]
    public async Task TransactionEvents_TransferMemo()
    {
        // Arrange
        const string data = "704164616d2042696c6c696f6e61697265";
        var address = AccountAddressHelper.CreateOneFilledWith(1);
        var transfer = new AccountTransfer(CcdAmount.Zero, address, OnChainData.FromHex(data));
        
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(transfer)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        await using var context = _dbContextFactory.CreateDbContext();
        
        var transaction = context.Transactions.Single();
        
        var events = await context.TransactionResultEvents.ToListAsync();
        events.Count.Should().Be(2);
        var first = events[0];
        first.TransactionId.Should().Be(transaction.Id);
        first.Entity.Should().BeOfType<Application.Api.GraphQL.Transactions.Transferred>();
        var second = events[1];
        second.TransactionId.Should().Be(transaction.Id);
        second.Entity.Should().BeOfType<TransferMemo>();
        var transferMemo = second.Entity as TransferMemo;
        transferMemo!.RawHex.Should().Be(data);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_MinBlockTimeUpdate()
    {
        // Arrange
        const int seconds = 1624630671;
        var duration = TimeSpan.FromSeconds(42);
        var update = new Concordium.Sdk.Types.MinBlockTimeUpdate(duration);
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<MinBlockTimeUpdate>(result.Payload);
        item.DurationSeconds.Should().Be((ulong)duration.TotalSeconds);
    }
    
    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_TimeoutParametersUpdate()
    {
        // Arrange
        const int seconds = 1624630671;
        var duration = TimeSpan.FromSeconds(42);
        var increase = new Concordium.Sdk.Types.Ratio(1, 2);
        var decrease = new Concordium.Sdk.Types.Ratio(3, 4);
        var update = new Concordium.Sdk.Types.TimeoutParametersUpdate(new TimeoutParameters(duration, increase, decrease));
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<TimeoutParametersUpdate>(result.Payload);
        item.Decrease.Denominator.Should().Be(decrease.Denominator);
        item.Decrease.Numerator.Should().Be(decrease.Numerator);
        item.Increase.Denominator.Should().Be(increase.Denominator);
        item.Increase.Numerator.Should().Be(increase.Numerator);
        item.DurationSeconds.Should().Be((ulong)duration.TotalSeconds);
    }
    
    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_FinalizationCommitteeParametersUpdate()
    {
        // Arrange
        const int seconds = 1624630671;
        const uint minFinalizers = 42;
        const uint maxFinalizers = 24;
        const decimal threshold = 0.42m;
        var update = new Concordium.Sdk.Types.FinalizationCommitteeParametersUpdate(
            new FinalizationCommitteeParameters(
                minFinalizers, maxFinalizers, AmountFraction.From(threshold)));
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<FinalizationCommitteeParametersUpdate>(result.Payload);
        item.MinFinalizers.Should().Be(minFinalizers);
        item.MaxFinalizers.Should().Be(maxFinalizers);
        item.FinalizersRelativeStakeThreshold.Should().Be(threshold);
    }
    
    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_BlockEnergyLimitUpdate()
    {
        // Arrange
        const int seconds = 1624630671;
        const ulong energyAmount = 42;
        var update = new Concordium.Sdk.Types.BlockEnergyLimitUpdate(new EnergyAmount(42));
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<BlockEnergyLimitUpdate>(result.Payload);
        item.EnergyLimit.Should().Be(energyAmount);
    }
    
    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_GasRewardsCpv2Update()
    {
        // Arrange
        const int seconds = 1624630671;
        const decimal baker = 0.42m;
        const decimal accountCreation = 0.43m;
        const decimal chainUpdate = 0.44m;
        var update = new Concordium.Sdk.Types.GasRewardsCpv2Update(
            AmountFraction.From(baker), 
            AmountFraction.From(accountCreation),
            AmountFraction.From(chainUpdate));
        
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<GasRewardsCpv2Update>(result.Payload);
        item.Baker.Should().Be(baker);
        item.AccountCreation.Should().Be(accountCreation);
        item.ChainUpdate.Should().Be(chainUpdate);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_MicroGtuPerEuroPayload() 
    {
        const int seconds = 1624630671;
        var microCcdPerEuroUpdate = new MicroCcdPerEuroUpdate(new ExchangeRate(1,2));

        var updateDetails = new UpdateDetailsBuilder(microCcdPerEuroUpdate)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<MicroCcdPerEuroChainUpdatePayload>(result.Payload);
        item.ExchangeRate.Numerator.Should().Be(1);
        item.ExchangeRate.Denominator.Should().Be(2);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_BakerStakeThresholdV1UpdatePayload() 
    {
        // Arrange
        const int seconds = 1624630671;
        const int amount = 12345;
        var bakerStakeThresholdUpdate = new BakerStakeThresholdUpdate(CcdAmount.FromMicroCcd(amount));

        var updateDetails = new UpdateDetailsBuilder(bakerStakeThresholdUpdate)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<BakerStakeThresholdChainUpdatePayload>(result.Payload);
        item.Amount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_CooldownParametersUpdatePayload() 
    {
        // Arrange
        const int seconds = 1624630671;
        var cooldownUpdate = new CooldownParametersCpv1Update(new CooldownParameters(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40)));
        
        var updateDetails = new UpdateDetailsBuilder(cooldownUpdate)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();
        
        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<CooldownParametersChainUpdatePayload>(result.Payload);
        item.PoolOwnerCooldown.Should().Be(20);
        item.DelegatorCooldown.Should().Be(40);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_PoolParametersUpdatePayload() 
    {
        // Arrange
        const int seconds = 1624630671;
        const decimal passiveFinalizationCommission = 0.1m;
        const decimal passiveBakingCommission = 0.2m;
        const decimal passiveTransactionCommission = 0.3m;
        const decimal finalizationMin = 1.0m;
        const decimal finalizationMax = 1.2m;
        const decimal bakingMin = 2.0m;
        const decimal bakingMax = 2.2m;
        const decimal transactionMin = 3.0m;
        const decimal transactionMax = 3.2m;
        const ulong amount = 12000UL;
        const decimal capitalBound = 3.0m;
        const ulong leverageFactorNumerator = 13UL;
        const ulong leverageFactorDenominator = 17UL;
        var update = new PoolParametersCpv1Update(
            new PoolParameters(
                AmountFraction.From(passiveFinalizationCommission),
                AmountFraction.From(passiveBakingCommission),
                AmountFraction.From(passiveTransactionCommission),
                new CommissionRanges(
                    new InclusiveRange<AmountFraction>(
                        AmountFraction.From(finalizationMin),
                        AmountFraction.From(finalizationMax)),
                    new InclusiveRange<AmountFraction>(
                        AmountFraction.From(bakingMin),
                        AmountFraction.From(bakingMax)),
                    new InclusiveRange<AmountFraction>(
                        AmountFraction.From(transactionMin),
                        AmountFraction.From(transactionMax))
                ),
                CcdAmount.FromMicroCcd(amount),
                new CapitalBound(AmountFraction.From(capitalBound)),
                new LeverageFactor(leverageFactorNumerator, leverageFactorDenominator)
            ));
        
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(DateTimeOffset.FromUnixTimeSeconds(seconds))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(seconds));
        var item = Assert.IsType<PoolParametersChainUpdatePayload>(result.Payload);
        item.PassiveFinalizationCommission.Should().Be(passiveFinalizationCommission);
        item.PassiveBakingCommission.Should().Be(passiveBakingCommission);
        item.PassiveTransactionCommission.Should().Be(passiveTransactionCommission);
        item.FinalizationCommissionRange.Min.Should().Be(finalizationMin);
        item.FinalizationCommissionRange.Max.Should().Be(finalizationMax);
        item.BakingCommissionRange.Min.Should().Be(bakingMin);
        item.BakingCommissionRange.Max.Should().Be(bakingMax);
        item.TransactionCommissionRange.Min.Should().Be(transactionMin);
        item.TransactionCommissionRange.Max.Should().Be(transactionMax);
        item.MinimumEquityCapital.Should().Be(amount);
        item.CapitalBound.Should().Be(capitalBound);
        item.LeverageBound.Numerator.Should().Be(leverageFactorNumerator);
        item.LeverageBound.Denominator.Should().Be(leverageFactorDenominator);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_TimeParametersUpdatePayload()
    {
        // Arrange
        var effectiveTime = DateTimeOffset.FromUnixTimeSeconds(1624630671);
        const ulong epoch = 170UL;
        const decimal mintPrPayDay = 4.2m;
        var mintRate = MintRateExtensions.From(mintPrPayDay);
        var update = new TimeParametersCpv1Update(
            new TimeParameters(
                new RewardPeriodLength(new Epoch(epoch)),
                mintRate
            )
        );
        
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(effectiveTime)
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(effectiveTime);
        var item = Assert.IsType<TimeParametersChainUpdatePayload>(result.Payload);
        item.RewardPeriodLength.Should().Be(epoch);
        item.MintPerPayday.Should().Be(mintPrPayDay);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_MintDistributionV1UpdatePayload()
    {
        // Arrange
        const decimal bakingReward = 1.1m;
        const decimal finalizationReward = 0.5m;
        var update = new MintDistributionCpv1Update(
            AmountFraction.From(bakingReward),
            AmountFraction.From(finalizationReward)
        );
        var effectiveTime = DateTimeOffset.FromUnixTimeSeconds(1624630671);
        var updateDetails = new UpdateDetailsBuilder(update)
            .WithEffectiveTime(effectiveTime)
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(updateDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(effectiveTime);
        var item = Assert.IsType<MintDistributionV1ChainUpdatePayload>(result.Payload);
        item.BakingReward.Should().Be(bakingReward);
        item.FinalizationReward.Should().Be(finalizationReward);
    }

    [Theory]
    [InlineData(BakerPoolOpenStatus.OpenForAll, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus.OpenForAll)]
    [InlineData(BakerPoolOpenStatus.ClosedForNew, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus.ClosedForNew)]
    [InlineData(BakerPoolOpenStatus.ClosedForAll, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus.ClosedForAll)]
    public async Task TransactionEvents_BakerSetOpenStatus(BakerPoolOpenStatus inputStatus, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus expectedStatus) 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong bakerId = 42UL;
        var update = new BakerConfigured(
            new List<IBakerEvent>{
                new BakerSetOpenStatusEvent(
                    new BakerId(new AccountIndex(bakerId)),
                    inputStatus
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<BakerSetOpenStatus>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.OpenStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetTransactionFeeCommission() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong bakerId = 42UL;
        const decimal transactionFeeCommission = 0.9m;
        var update = new BakerConfigured(
            new List<IBakerEvent>{
                new BakerSetTransactionFeeCommissionEvent(
                    new BakerId(new AccountIndex(bakerId)),
                    AmountFraction.From(transactionFeeCommission)
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<BakerSetTransactionFeeCommission>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.TransactionFeeCommission.Should().Be(transactionFeeCommission);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetMetadataURL() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong bakerId = 42UL;
        const string metaDataUrl = "https://ccd.bakers.com/metadata";
        var update = new BakerConfigured(
            new List<IBakerEvent>{
                new BakerSetMetadataUrlEvent(
                    new BakerId(new AccountIndex(bakerId)),
                    metaDataUrl
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<BakerSetMetadataURL>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.MetadataUrl.Should().Be(metaDataUrl);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetBakingRewardCommission() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong bakerId = 42UL;
        const decimal bakerRewardCommission = 0.9m;
        var update = new BakerConfigured(
            new List<IBakerEvent>{
                new BakerSetBakingRewardCommissionEvent(
                    new BakerId(new AccountIndex(bakerId)),
                    AmountFraction.From(bakerRewardCommission)
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<BakerSetBakingRewardCommission>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.BakingRewardCommission.Should().Be(bakerRewardCommission);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetFinalizationRewardCommission() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong bakerId = 42UL;
        const decimal finalizationRewardCommission = 0.9m;
        var update = new BakerConfigured(
            new List<IBakerEvent>{
                new BakerSetFinalizationRewardCommissionEvent(
                    new BakerId(new AccountIndex(bakerId)),
                    AmountFraction.From(finalizationRewardCommission)
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<BakerSetFinalizationRewardCommission>();
        result.BakerId.Should().Be(bakerId);
        result.AccountAddress.AsString.Should().Be(address);
        result.FinalizationRewardCommission.Should().Be(finalizationRewardCommission);
    }

    [Fact]
    public async Task TransactionEvents_DelegationAdded() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong delegationId = 42UL;
        var update = new DelegationConfigured(
            new List<IDelegationEvent>{
                new DelegationAdded(
                    new DelegatorId(new AccountIndex(delegationId))
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationAdded>();
        result.DelegatorId.Should().Be(delegationId);
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionEvents_DelegationRemoved() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong delegationId = 42UL;
        var update = new DelegationConfigured(
            new List<IDelegationEvent>{
                new DelegationRemoved(
                    new DelegatorId(new AccountIndex(delegationId))
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationRemoved>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_DelegationStakeIncreased() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong delegationId = 42UL;
        const ulong amount = 758111UL;
        var update = new DelegationConfigured(
            new List<IDelegationEvent>{
                new DelegationStakeIncreased(
                    new DelegatorId(new AccountIndex(delegationId)),
                    CcdAmount.FromMicroCcd(amount)
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationStakeIncreased>();
        result.DelegatorId.Should().Be(delegationId);
        result.AccountAddress.AsString.Should().Be(address);
        result.NewStakedAmount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionEvents_DelegationStakeDecreased() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong delegationId = 42UL;
        const ulong amount = 758111UL;
        var update = new DelegationConfigured(
            new List<IDelegationEvent>{
                new DelegationStakeDecreased(
                    new DelegatorId(new AccountIndex(delegationId)),
                    CcdAmount.FromMicroCcd(amount)
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary});

        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationStakeDecreased>();
        result.DelegatorId.Should().Be(delegationId);
        result.AccountAddress.AsString.Should().Be(address);
        result.NewStakedAmount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionEvents_DelegationSetRestakeEarnings() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong delegationId = 42UL;
        const bool restakeEarnings = true;
        var update = new DelegationConfigured(
            new List<IDelegationEvent>{
                new DelegationSetRestakeEarnings(
                    new DelegatorId(new AccountIndex(delegationId)),
                    restakeEarnings
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary}); 
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationSetRestakeEarnings>();
        result.DelegatorId.Should().Be(delegationId);
        result.AccountAddress.AsString.Should().Be(address);
        result.RestakeEarnings.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionEvents_DelegationSetDelegationTarget() 
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const ulong delegationId = 42UL;
        var update = new DelegationConfigured(
            new List<IDelegationEvent>{
                new DelegationSetDelegationTarget(
                    new DelegatorId(new AccountIndex(delegationId)),
                    new PassiveDelegationTarget()
                )});
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(update)
            .WithSender(AccountAddress.From(address))
            .Build();
        
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();

        // Act
        await WriteData(new List<BlockItemSummary>{blockItemSummary}); 
        
        // Assert
        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationSetDelegationTarget>();
        result.DelegatorId.Should().Be(delegationId);
        result.AccountAddress.AsString.Should().Be(address);
        result.DelegationTarget.Should().BeOfType<Application.Api.GraphQL.PassiveDelegationTarget>();
    }

    [Fact]
    public async Task TransactionRejectReason_ModuleNotWf()
    {
        // Arrange
        var moduleNotWf = new ModuleNotWf();
        
        // Act
        await WriteSingleRejectedTransaction(moduleNotWf);

        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ModuleNotWf>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_ModuleHashAlreadyExists()
    {
        // Arrange
        const string hexString = "2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb";
        var reason = new ModuleHashAlreadyExists(new ModuleReference(hexString));

        // Act
        await WriteSingleRejectedTransaction(reason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ModuleHashAlreadyExists>();
        result.ModuleRef.Should().Be(hexString);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidAccountReference()
    {
        // Assert
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var inputReason = new InvalidAccountReference(AccountAddress.From(address));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidAccountReference>();
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidInitMethod()
    {
        // Assert
        const string moduleRef = "2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb";
        const string name = "init_trader";
        var _ = ContractName.TryParse(name, out var output);
        var inputReason = new InvalidInitMethod(new ModuleReference(moduleRef), output.ContractName!);
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidInitMethod>();
        result.ModuleRef.Should().Be(moduleRef);
        result.InitName.Should().Be(name);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidReceiveMethod()
    {
        // Assert
        const string moduleRef = "2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb";
        const string name = "trader.receive";
        var _ = ReceiveName.TryParse(name, out var output);
        var inputReason = new InvalidReceiveMethod(new ModuleReference(moduleRef), output.ReceiveName!);
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidReceiveMethod>();
        result.ModuleRef.Should().Be(moduleRef);
        result.ReceiveName.Should().Be(name);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidModuleReference()
    {
        // Arrange
        const string moduleRef = "2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb";
        var inputReason = new InvalidModuleReference(new ModuleReference(moduleRef));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidModuleReference>();
        result.ModuleRef.Should().Be(moduleRef);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidContractAddress()
    {
        // Arrange
        const ulong index = 187;
        const ulong subIndex = 22;
        var inputReason = new InvalidContractAddress(ContractAddress.From(index, subIndex));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidContractAddress>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
    }

    [Fact]
    public async Task TransactionRejectReason_RuntimeFailure()
    {
        // Arrange
        var inputReason = new RuntimeFailure();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RuntimeFailure>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_AmountTooLarge()
    {
        // Arrange
        const ulong amount = 34656UL;
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var inputReason = new AmountTooLarge(AccountAddress.From(address), CcdAmount.FromMicroCcd(amount));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.AmountTooLarge>();
        result.Address.Should().Be(new Application.Api.GraphQL.Accounts.AccountAddress(address));
        result.Amount.Should().Be(amount);
    }

    [Fact]
    public async Task TransactionRejectReason_SerializationFailure()
    {
        // Arrange
        var inputReason = new SerializationFailure();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.SerializationFailure>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_OutOfEnergy()
    {
        // Arrange
        var inputReason = new OutOfEnergy();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.OutOfEnergy>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_RejectedInit()
    {
        // Arrange
        const int rejectReason = -48518;
        var inputReason = new RejectedInit(rejectReason);
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RejectedInit>();
        result.RejectReason.Should().Be(rejectReason);
    }

    [Fact]
    public async Task TransactionRejectReason_RejectedReceive()
    {
        // Arrange
        const int rejectReason = -48518;
        const ulong index = 187UL;
        const ulong subIndex = 22UL;
        const string name = "trader.dostuff";
        const string parameter = "fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00";
        var _ = ReceiveName.TryParse(name, out var output);
        var inputReason = new RejectedReceive(
            rejectReason,
            ContractAddress.From(index, subIndex),
            output.ReceiveName!,
            new Parameter(Convert.FromHexString(parameter)));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RejectedReceive>();
        result.RejectReason.Should().Be(rejectReason);
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(index, subIndex));
        result.ReceiveName.Should().Be(name);
        result.MessageAsHex.Should().Be(parameter);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidProof()
    {
        // Arrange
        var inputReason = new InvalidProof();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidProof>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_AlreadyABaker()
    {
        // Arrange
        const ulong bakerId = 45UL;
        var inputReason = new AlreadyABaker(new BakerId(new AccountIndex(bakerId)));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.AlreadyABaker>();
        result.BakerId.Should().Be(bakerId);
    }

    [Fact]
    public async Task TransactionRejectReason_NotABaker()
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var inputReason = new NotABaker(AccountAddress.From(address));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotABaker>();
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionRejectReason_InsufficientBalanceForBakerStake()
    {
        // Arrange
        var inputReason = new InsufficientBalanceForBakerStake();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InsufficientBalanceForBakerStake>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_StakeUnderMinimumThresholdForBaking()
    {
        // Arrange
        var inputReason = new StakeUnderMinimumThresholdForBaking();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.StakeUnderMinimumThresholdForBaking>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_BakerInCooldown()
    {
        // Arrange
        var inputReason = new BakerInCooldown();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.BakerInCooldown>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_DuplicateAggregationKey()
    {
        // Arrange
        const string key = "98528ef89dc117f102ef3f089c81b92e4d945d22c0269269af6ef9f876d79e828b31b8b4b8cc3d9234c30e83bd79e20a0a807bc110f0ac9babae90cb6a8c6d0deb2e5627704b41bdd646a547895fd1f9f2a7b0dd4fb4e138356e91d002a28f83";
        var inputReason = new DuplicateAggregationKey(Convert.FromHexString(key));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DuplicateAggregationKey>();
        result.AggregationKey.Should().Be(key);
    }

    [Fact]
    public async Task TransactionRejectReason_NonExistentCredentialId()
    {
        // Arrange
        var inputReason = new NonExistentCredentialId();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NonExistentCredentialId>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_KeyIndexAlreadyInUse()
    {
        // Arrange
        var inputReason = new KeyIndexAlreadyInUse();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.KeyIndexAlreadyInUse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidAccountThreshold()
    {
        // Arrange
        var inputReason = new InvalidAccountThreshold();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidAccountThreshold>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidCredentialKeySignThreshold()
    {
        // Arrange
        var inputReason = new InvalidCredentialKeySignThreshold();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidCredentialKeySignThreshold>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidEncryptedAmountTransferProof()
    {
        // Arrange
        var inputReason = new InvalidEncryptedAmountTransferProof();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidEncryptedAmountTransferProof>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidTransferToPublicProof()
    {
        // Arrange
        var inputReason = new InvalidTransferToPublicProof();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidTransferToPublicProof>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_EncryptedAmountSelfTransfer()
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var inputReason = new EncryptedAmountSelfTransfer(AccountAddress.From(address));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.EncryptedAmountSelfTransfer>();
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidIndexOnEncryptedTransfer()
    {
        // Arrange
        var inputReason = new InvalidIndexOnEncryptedTransfer();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidIndexOnEncryptedTransfer>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_ZeroScheduledAmount()
    {
        // Arrange
        var inputReason = new ZeroScheduledAmount();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ZeroScheduledAmount>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NonIncreasingSchedule()
    {
        // Arrange
        var inputReason = new NonIncreasingSchedule();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NonIncreasingSchedule>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_FirstScheduledReleaseExpired()
    {
        // Arrange
        var inputReason = new FirstScheduledReleaseExpired();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.FirstScheduledReleaseExpired>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_ScheduledSelfTransfer()
    {
        // Arrange
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var inputReason = new ScheduledSelfTransfer(AccountAddress.From(address));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ScheduledSelfTransfer>();
        result.AccountAddress.AsString.Should().Be(address);
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidCredentials()
    {
        // Arrange
        var inputReason = new InvalidCredentials();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidCredentials>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_DuplicateCredIds()
    {
        // Arrange
        const string credId = "b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f";
        var inputReason = new DuplicateCredIds(new List<byte[]>{Convert.FromHexString(credId)});
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DuplicateCredIds>();
        result.CredIds.Should().ContainSingle().Which.Should().Be(credId);
    }

    [Fact]
    public async Task TransactionRejectReason_NonExistentCredIds()
    {
        // Arrange
        const string credId = "b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f";
        var inputReason = new NonExistentCredIds(new List<byte[]> { Convert.FromHexString(credId) });
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NonExistentCredIds>();
        result.CredIds.Should().ContainSingle().Which.Should().Be(credId);
    }

    [Fact]
    public async Task TransactionRejectReason_RemoveFirstCredential()
    {
        // Arrange
        var inputReason = new RemoveFirstCredential();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RemoveFirstCredential>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_CredentialHolderDidNotSign()
    {
        // Arrange
        var inputReason = new CredentialHolderDidNotSign();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.CredentialHolderDidNotSign>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NotAllowedMultipleCredentials()
    {
        // Arrange
        var inputReason = new NotAllowedMultipleCredentials();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotAllowedMultipleCredentials>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NotAllowedToReceiveEncrypted()
    {
        // Arrange
        var inputReason = new NotAllowedToReceiveEncrypted();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotAllowedToReceiveEncrypted>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NotAllowedToHandleEncrypted()
    {
        // Arrange
        var inputReason = new NotAllowedToHandleEncrypted();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotAllowedToHandleEncrypted>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_MissingBakerAddParameters()
    {
        // Arrange
        var inputReason = new MissingBakerAddParameters();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.MissingBakerAddParameters>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_FinalizationRewardCommissionNotInRange()
    {
        // Arrange
        var inputReason = new FinalizationRewardCommissionNotInRange();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.FinalizationRewardCommissionNotInRange>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_BakingRewardCommissionNotInRange()
    {
        // Arrange
        var inputReason = new BakingRewardCommissionNotInRange();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.BakingRewardCommissionNotInRange>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_TransactionFeeCommissionNotInRange()
    {
        // Arrange
        var inputReason = new TransactionFeeCommissionNotInRange();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.TransactionFeeCommissionNotInRange>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_AlreadyADelegator()
    {
        // Arrange
        var inputReason = new AlreadyADelegator();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.AlreadyADelegator>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_InsufficientBalanceForDelegationStake()
    {
        // Arrange
        var inputReason = new InsufficientBalanceForDelegationStake();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InsufficientBalanceForDelegationStake>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_MissingDelegationAddParameters()
    {
        // Arrange
        var inputReason = new Concordium.Sdk.Types.MissingDelegationAddParameters();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<MissingDelegationAddParameters>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_InsufficientDelegationStake()
    {
        // Arrange
        var inputReason = new InsufficientDelegationStake();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InsufficientDelegationStake>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_DelegatorInCooldown()
    {
        // Arrange
        var inputReason = new DelegatorInCooldown();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DelegatorInCooldown>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_NotADelegator()
    {
        // Arrange
        var address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var inputReason = new NotADelegator(AccountAddress.From(address));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotADelegator>();
        result.Should().NotBeNull();
        result.AccountAddress.AsString.Should().Be(address);
    }
    
    [Fact]
    public async Task TransactionRejectReason_DelegationTargetNotABaker()
    {
        // Arrange
        const ulong bakerId = 42UL;
        var inputReason = new DelegationTargetNotABaker(new BakerId(new AccountIndex(bakerId)));
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DelegationTargetNotABaker>();
        result.Should().NotBeNull();
        result.BakerId.Should().Be(bakerId);
    }
    
    [Fact]
    public async Task TransactionRejectReason_StakeOverMaximumThresholdForPool()
    {
        // Arrange
        var inputReason = new StakeOverMaximumThresholdForPool();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.StakeOverMaximumThresholdForPool>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_PoolWouldBecomeOverDelegated()
    {
        // Arrange
        var inputReason = new PoolWouldBecomeOverDelegated();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Act
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.PoolWouldBecomeOverDelegated>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_PoolClosed()
    {
        // Arrange
        var inputReason = new PoolClosed();
        
        // Act
        await WriteSingleRejectedTransaction(inputReason);
        
        // Assert
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.PoolClosed>();
        result.Should().NotBeNull();
    }
    
    private async Task WriteData(List<BlockItemSummary> blockItemSummaries, long blockId = 42)
    {
        await _target.AddTransactions(blockItemSummaries, blockId, _anyBlockSlotTime);
    }
    
    private async Task<T> ReadSingleTransactionEventType<T>()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.TransactionResultEvents.SingleAsync();
        return result.Entity.Should().BeOfType<T>().Subject;
    }
    
    private async Task WriteSingleRejectedTransaction(IRejectReason rejectReason)
    {
        var none = new None(null, rejectReason);
        var accountTransactionDetails = new AccountTransactionDetailsBuilder(none)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
            .Build();
        
        await WriteData(new List<BlockItemSummary>{blockItemSummary});
    }
    
    private async Task<T> ReadSingleRejectedTransactionRejectReason<T>() where T : TransactionRejectReason
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = await dbContext.Transactions.SingleAsync();
        var rejected = Assert.IsType<Rejected>(transaction.Result);
        return Assert.IsType<T>(rejected.Reason);
    }
}
