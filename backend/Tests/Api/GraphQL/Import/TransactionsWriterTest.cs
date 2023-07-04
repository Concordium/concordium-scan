using System.Collections.Generic;
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
using AccountTransaction = Application.Api.GraphQL.Transactions.AccountTransaction;
using ContractAddress = Concordium.Sdk.Types.ContractAddress;
using TransactionHash = Concordium.Sdk.Types.TransactionHash;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class TransactionsWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly TransactionWriter _target;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyBlockSlotTime = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public TransactionsWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new TransactionWriter(_dbContextFactory, new NullMetrics());

        using var connection = dbFixture.GetOpenConnection();
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

    // [Theory]
    // [InlineData(TransactionType.AddBaker)]
    // [InlineData(TransactionType.EncryptedAmountTransfer)]
    // [InlineData(TransactionType.Transfer)]
    // [InlineData(TransactionType.TransferWithSchedule)]
    // [InlineData(TransactionType.InitContract)]
    // public async Task Transactions_TransactionType_TransactionTypes(TransactionType transactionType)
    // {
    //     var dataRegistered = new Concordium.Sdk.Types.DataRegistered(Array.Empty<byte>());
    //     var accountTransactionDetails = new AccountTransactionDetailsBuilder(dataRegistered)
    //         .Build();
    //     var blockItemSummary = new BlockItemSummaryBuilder(accountTransactionDetails)
    //         .Build();
    //
    //     await WriteData(blockItemSummary);
    //     
    //     await using var dbContext = _dbContextFactory.CreateDbContext();
    //     var transaction = dbContext.Transactions.Single();
    //     transaction.TransactionType.Should().BeOfType<AccountTransaction>()
    //         .Which.AccountTransactionType.Should().Be(transactionType);
    // }
    
    [Fact]
    public async Task TransactionEvents_TransactionIdAndIndex()
    {
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";

        var accountCreationDetails = new AccountCreationDetailsBuilder(CredentialType.Normal)
            .WithCredentialRegistrationId(new CredentialRegistrationId(Convert.FromHexString("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d")))
            .WithAccountAddress(AccountAddress.From(address))
            .Build();
        var blockItemSummaryCredentialDeployed = new BlockItemSummaryBuilder(accountCreationDetails)
            .Build();
        
        var accountCreated = new AccountCreationDetailsBuilder(CredentialType.Initial)
            .WithAccountAddress(AccountAddress.From(address))
            .Build();
        var blockItemSummaryAccountCreated = new BlockItemSummaryBuilder(accountCreated)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummaryCredentialDeployed, blockItemSummaryAccountCreated});

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();

        var result = dbContext.TransactionResultEvents.ToArray();
        result.Length.Should().Be(2);
        result[0].TransactionId.Should().Be(transaction.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Should().BeOfType<CredentialDeployed>();
        result[1].TransactionId.Should().Be(transaction.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Should().BeOfType<AccountCreated>();
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
    public async Task TransactionEvents_AccountCreated()
    {
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        var accountCreated = new AccountCreationDetailsBuilder(CredentialType.Initial)
            .WithAccountAddress(AccountAddress.From(address))
            .Build();
        var blockItemSummaryAccountCreated = new BlockItemSummaryBuilder(accountCreated)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummaryAccountCreated});

        var result = await ReadSingleTransactionEventType<AccountCreated>();
        result.AccountAddress.AsString.Should().Be(address);
    }
    
    [Fact]
    public async Task TransactionEvents_CredentialDeployed()
    {
        const string address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
        const string regId = "b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d";

        var accountCreationDetails = new AccountCreationDetailsBuilder(CredentialType.Normal)
            .WithCredentialRegistrationId(new CredentialRegistrationId(Convert.FromHexString(regId)))
            .WithAccountAddress(AccountAddress.From(address))
            .Build();
        var blockItemSummaryCredentialDeployed = new BlockItemSummaryBuilder(accountCreationDetails)
            .Build();

        await WriteData(new List<BlockItemSummary>{blockItemSummaryCredentialDeployed});

        var result = await ReadSingleTransactionEventType<CredentialDeployed>();
        result.RegId.Should().Be(regId);
        result.AccountAddress.AsString.Should().Be(address);
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
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerRemoved(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 21))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.BakerRemoved>();
        result.BakerId.Should().Be(21);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_BakerSetRestakeEarnings()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerSetRestakeEarnings(23, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), true))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerSetRestakeEarnings>();
        result.BakerId.Should().Be(23);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.RestakeEarnings.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionEvents_BakerStakeDecreased()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerStakeDecreased(23, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(34786451)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerStakeDecreased>();
        result.BakerId.Should().Be(23);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewStakedAmount.Should().Be(34786451);
    }

    [Fact]
    public async Task TransactionEvents_BakerStakeIncreased()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerStakeIncreased(23, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(34786451)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerStakeIncreased>();
        result.BakerId.Should().Be(23);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewStakedAmount.Should().Be(34786451);
    }

    [Fact]
    public async Task TransactionEvents_AmountAddedByDecryption()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.AmountAddedByDecryption(CcdAmount.FromMicroCcd(2362462), AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<AmountAddedByDecryption>();
        result.Amount.Should().Be(2362462);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_EncryptedAmountsRemoved()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.EncryptedAmountsRemoved(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2", "acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074", 789))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<EncryptedAmountsRemoved>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewEncryptedAmount.Should().Be("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2");
        result.InputAmount.Should().Be("acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074");
        result.UpToIndex.Should().Be(789);
    }

    [Fact]
    public async Task TransactionEvents_EncryptedSelfAmountAdded()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.EncryptedSelfAmountAdded(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2", CcdAmount.FromMicroCcd(23446)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<EncryptedSelfAmountAdded>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewEncryptedAmount.Should().Be("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2");
        result.Amount.Should().Be(23446);
    }

    [Fact]
    public async Task TransactionEvents_NewEncryptedAmount()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.NewEncryptedAmount(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 155, "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<NewEncryptedAmount>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewIndex.Should().Be(155);
        result.EncryptedAmount.Should().Be("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2");
    }

    [Fact]
    public async Task TransactionEvents_CredentialKeysUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.CredentialKeysUpdated("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.CredentialKeysUpdated>();
        result.CredId.Should().Be("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d");
    }

    [Fact]
    public async Task TransactionEvents_CredentialsUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.CredentialsUpdated(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), new []{"b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d"}, new string[0], 123))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.CredentialsUpdated>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewCredIds.Should().Equal("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d");
        result.RemovedCredIds.Should().BeEmpty();
        result.NewThreshold.Should().Be(123);
    }

    [Fact]
    public async Task TransactionEvents_ContractInitialized()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.ContractInitialized(new ModuleReference("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"), ContractAddress.From(1423, 1), CcdAmount.FromMicroCcd(5345462), "init_CIS1-singleNFT", new []{ BinaryData.FromHexString("fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4"), BinaryData.FromHexString("fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00")}))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.ContractInitialized>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.Amount.Should().Be(5345462);
        result.InitName.Should().Be("init_CIS1-singleNFT");
        result.EventsAsHex.Should().Equal("fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4", "fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00");
    }

    [Fact]
    public async Task TransactionEvents_ContractModuleDeployed()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new ModuleDeployed(new ModuleReference("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ContractModuleDeployed>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
    }

    [Fact]
    public async Task TransactionEvents_ContractUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Updated(
                        ContractAddress.From(1423, 1),
                        AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
                        CcdAmount.FromMicroCcd(15674371),
                        BinaryData.FromHexString("080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"), 
                        "inventory.transfer", 
                        new []
                        {
                            BinaryData.FromHexString("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"),
                            BinaryData.FromHexString("01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32")
                        }))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ContractUpdated>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.Instigator.Should().Be(new Application.Api.GraphQL.Accounts.AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        result.Amount.Should().Be(15674371);
        result.MessageAsHex.Should().Be("080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32");
        result.ReceiveName.Should().Be("inventory.transfer");
        result.EventsAsHex.Should().Equal("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32", "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32");
    }

    [Fact]
    public async Task TransactionEvents_ContractInterrupted()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Interrupted(
                        ContractAddress.From(1423, 1),
                        new []
                        {
                            BinaryData.FromHexString("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"),
                            BinaryData.FromHexString("01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32")
                        }))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ContractInterrupted>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.EventsAsHex.Should().Equal("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32", "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32");
    }

    [Fact]
    public async Task TransactionEvents_ContractResumed()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Resumed(
                        ContractAddress.From(1423, 1),
                        true))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ContractResumed>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.Success.Should().BeTrue();
    }


    [Fact]
    public async Task TransactionEvents_ContractUpgraded()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Upgraded(
                        ContractAddress.From(1423, 1),
                        new ModuleReference("73ba390d9ce2bb1bf54f124bb00e9dee0d6dc40d6de0f5ba06e1d1f095e4afcc"),
                        new ModuleReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                    ))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ContractUpgraded>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.From.Should().Be("73ba390d9ce2bb1bf54f124bb00e9dee0d6dc40d6de0f5ba06e1d1f095e4afcc");
        result.To.Should().Be("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    }

    [Fact]
    public async Task TransactionEvents_TransferredWithSchedule()
    {
        var baseTimestamp = new DateTimeOffset(2010, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.TransferredWithSchedule(
                        AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 
                        AccountAddress.From("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH"), 
                        new []
                        {
                            new TimestampedAmount(baseTimestamp.AddHours(10), CcdAmount.FromMicroCcd(1000)),
                            new TimestampedAmount(baseTimestamp.AddHours(20), CcdAmount.FromMicroCcd(3333)),
                            new TimestampedAmount(baseTimestamp.AddHours(30), CcdAmount.FromMicroCcd(2111)),
                        }))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.TransferredWithSchedule>();
        result.FromAccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.ToAccountAddress.AsString.Should().Be("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH");
        result.AmountsSchedule.Should().Equal(
            new Application.Api.GraphQL.TimestampedAmount(baseTimestamp.AddHours(10), 1000),
            new Application.Api.GraphQL.TimestampedAmount(baseTimestamp.AddHours(20), 3333),
            new Application.Api.GraphQL.TimestampedAmount(baseTimestamp.AddHours(30), 2111));
    }
    
    [Fact]
    public async Task TransactionEvents_DataRegistered()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DataRegistered(RegisteredData.FromHexString("784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DataRegistered>();
        result.DataAsHex.Should().Be("784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863");
    }

    [Fact]
    public async Task TransactionEvents_TransferMemo()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.TransferMemo(Memo.CreateFromHex("704164616d2042696c6c696f6e61697265")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<TransferMemo>();
        result.RawHex.Should().Be("704164616d2042696c6c696f6e61697265");
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_MicroGtuPerEuroPayload() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), new MicroGtuPerEuroUpdatePayload(new ExchangeRate(1, 2))))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
        var item = Assert.IsType<MicroCcdPerEuroChainUpdatePayload>(result.Payload);
        item.ExchangeRate.Numerator.Should().Be(1);
        item.ExchangeRate.Denominator.Should().Be(2);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_BakerStakeThresholdV1UpdatePayload() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), new BakerStakeThresholdUpdatePayload(new BakerParameters(CcdAmount.FromMicroCcd(12345)))))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
        var item = Assert.IsType<BakerStakeThresholdChainUpdatePayload>(result.Payload);
        item.Amount.Should().Be(12345);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_CooldownParametersUpdatePayload() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), new CooldownParametersUpdatePayload(new CooldownParameters(20, 40))))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
        var item = Assert.IsType<CooldownParametersChainUpdatePayload>(result.Payload);
        item.PoolOwnerCooldown.Should().Be(20);
        item.DelegatorCooldown.Should().Be(40);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_PoolParametersUpdatePayload() 
    {
        var payload = new PoolParametersUpdatePayload(new PoolParameters(
            0.1m, 0.2m, 0.3m, 
            new InclusiveRange<decimal>(1.0m, 1.2m),
            new InclusiveRange<decimal>(2.0m, 2.2m),
            new InclusiveRange<decimal>(3.0m, 3.2m),
            CcdAmount.FromMicroCcd(12000), 3.0m, new LeverageFactor(13, 17)));
        
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), payload))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
        var item = Assert.IsType<PoolParametersChainUpdatePayload>(result.Payload);
        item.PassiveFinalizationCommission.Should().Be(0.1m);
        item.PassiveBakingCommission.Should().Be(0.2m);
        item.PassiveTransactionCommission.Should().Be(0.3m);
        item.FinalizationCommissionRange.Min.Should().Be(1.0m);
        item.FinalizationCommissionRange.Max.Should().Be(1.2m);
        item.BakingCommissionRange.Min.Should().Be(2.0m);
        item.BakingCommissionRange.Max.Should().Be(2.2m);
        item.TransactionCommissionRange.Min.Should().Be(3.0m);
        item.TransactionCommissionRange.Max.Should().Be(3.2m);
        item.MinimumEquityCapital.Should().Be(12000UL);
        item.CapitalBound.Should().Be(3.0m);
        item.LeverageBound.Numerator.Should().Be(13);
        item.LeverageBound.Denominator.Should().Be(17);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_TimeParametersUpdatePayload() 
    {
        var payload = new TimeParametersUpdatePayload(new TimeParameters(170, 4.2m));
        
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), payload))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
        var item = Assert.IsType<TimeParametersChainUpdatePayload>(result.Payload);
        item.RewardPeriodLength.Should().Be(170);
        item.MintPerPayday.Should().Be(4.2m);
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued_MintDistributionV1UpdatePayload() 
    {
        var payload = new MintDistributionV1UpdatePayload(new MintDistributionV1(1.1m, 0.5m));
        
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), payload))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
        var item = Assert.IsType<MintDistributionV1ChainUpdatePayload>(result.Payload);
        item.BakingReward.Should().Be(1.1m);
        item.FinalizationReward.Should().Be(0.5m);
    }

    [Theory]
    [InlineData(BakerPoolOpenStatus.OpenForAll, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus.OpenForAll)]
    [InlineData(BakerPoolOpenStatus.ClosedForNew, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus.ClosedForNew)]
    [InlineData(BakerPoolOpenStatus.ClosedForAll, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus.ClosedForAll)]
    public async Task TransactionEvents_BakerSetOpenStatus(BakerPoolOpenStatus inputStatus, Application.Api.GraphQL.Bakers.BakerPoolOpenStatus expectedStatus) 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerSetOpenStatus(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), inputStatus))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerSetOpenStatus>();
        result.BakerId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.OpenStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetTransactionFeeCommission() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerSetTransactionFeeCommission(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 0.9m))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerSetTransactionFeeCommission>();
        result.BakerId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.TransactionFeeCommission.Should().Be(0.9m);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetMetadataURL() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerSetMetadataURL(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "https://ccd.bakers.com/metadata"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerSetMetadataURL>();
        result.BakerId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.MetadataUrl.Should().Be("https://ccd.bakers.com/metadata");
    }

    [Fact]
    public async Task TransactionEvents_BakerSetBakingRewardCommission() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerSetBakingRewardCommission(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 0.9m))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerSetBakingRewardCommission>();
        result.BakerId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.BakingRewardCommission.Should().Be(0.9m);
    }

    [Fact]
    public async Task TransactionEvents_BakerSetFinalizationRewardCommission() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.BakerSetFinalizationRewardCommission(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 0.9m))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<BakerSetFinalizationRewardCommission>();
        result.BakerId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.FinalizationRewardCommission.Should().Be(0.9m);
    }

    [Fact]
    public async Task TransactionEvents_DelegationAdded() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DelegationAdded(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationAdded>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_DelegationRemoved() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DelegationRemoved(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationRemoved>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_DelegationStakeIncreased() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DelegationStakeIncreased(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(758111)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationStakeIncreased>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewStakedAmount.Should().Be(758111);
    }

    [Fact]
    public async Task TransactionEvents_DelegationStakeDecreased() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DelegationStakeDecreased(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(758111)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationStakeDecreased>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewStakedAmount.Should().Be(758111);
    }

    [Fact]
    public async Task TransactionEvents_DelegationSetRestakeEarnings() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DelegationSetRestakeEarnings(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), true))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationSetRestakeEarnings>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.RestakeEarnings.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionEvents_DelegationSetDelegationTarget() 
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Concordium.Sdk.Types.DelegationSetDelegationTarget(42, AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), new PassiveDelegationTarget()))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transactions.DelegationSetDelegationTarget>();
        result.DelegatorId.Should().Be(42);
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.DelegationTarget.Should().BeOfType<Application.Api.GraphQL.PassiveDelegationTarget>();
    }

    [Fact]
    public async Task TransactionRejectReason_ModuleNotWf()
    {
        var inputReason = new Application.NodeApi.ModuleNotWf();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ModuleNotWf>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_ModuleHashAlreadyExists()
    {
        var inputReason = new Application.NodeApi.ModuleHashAlreadyExists(new ModuleReference("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ModuleHashAlreadyExists>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidAccountReference()
    {
        var inputReason = new Application.NodeApi.InvalidAccountReference(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidAccountReference>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidInitMethod()
    {
        var inputReason = new Application.NodeApi.InvalidInitMethod(new ModuleReference("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"), "trader.init");
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidInitMethod>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
        result.InitName.Should().Be("trader.init");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidReceiveMethod()
    {
        var inputReason = new Application.NodeApi.InvalidReceiveMethod(new ModuleReference("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"), "trader.receive");
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidReceiveMethod>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
        result.ReceiveName.Should().Be("trader.receive");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidModuleReference()
    {
        var inputReason = new Application.NodeApi.InvalidModuleReference(new ModuleReference("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidModuleReference>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidContractAddress()
    {
        var inputReason = new Application.NodeApi.InvalidContractAddress(ContractAddress.From(187, 22));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidContractAddress>();
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(187, 22));
    }

    [Fact]
    public async Task TransactionRejectReason_RuntimeFailure()
    {
        var inputReason = new Application.NodeApi.RuntimeFailure();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RuntimeFailure>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_AmountTooLarge()
    {
        var inputReason = new Application.NodeApi.AmountTooLarge(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(34656));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.AmountTooLarge>();
        result.Address.Should().Be(new Application.Api.GraphQL.Accounts.AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        result.Amount.Should().Be(34656);
    }

    [Fact]
    public async Task TransactionRejectReason_SerializationFailure()
    {
        var inputReason = new Application.NodeApi.SerializationFailure();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.SerializationFailure>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_OutOfEnergy()
    {
        var inputReason = new Application.NodeApi.OutOfEnergy();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.OutOfEnergy>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_RejectedInit()
    {
        var inputReason = new Application.NodeApi.RejectedInit(-48518);
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RejectedInit>();
        result.RejectReason.Should().Be(-48518);
    }

    [Fact]
    public async Task TransactionRejectReason_RejectedReceive()
    {
        var inputReason = new Application.NodeApi.RejectedReceive(
            -48518,
            ContractAddress.From(187, 22),
            "trader.dostuff",
            BinaryData.FromHexString("fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00"));
        
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RejectedReceive>();
        result.RejectReason.Should().Be(-48518);
        result.ContractAddress.Should().Be(new Application.Api.GraphQL.ContractAddress(187, 22));
        result.ReceiveName.Should().Be("trader.dostuff");
        result.MessageAsHex.Should().Be("fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00");
    }

    [Fact]
    public async Task TransactionRejectReason_NonExistentRewardAccount()
    {
        var inputReason = new Application.NodeApi.NonExistentRewardAccount(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<NonExistentRewardAccount>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidProof()
    {
        var inputReason = new Application.NodeApi.InvalidProof();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidProof>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_AlreadyABaker()
    {
        var inputReason = new Application.NodeApi.AlreadyABaker(45);
        
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.AlreadyABaker>();
        result.BakerId.Should().Be(45);
    }

    [Fact]
    public async Task TransactionRejectReason_NotABaker()
    {
        var inputReason = new Application.NodeApi.NotABaker(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotABaker>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionRejectReason_InsufficientBalanceForBakerStake()
    {
        var inputReason = new Application.NodeApi.InsufficientBalanceForBakerStake();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InsufficientBalanceForBakerStake>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_StakeUnderMinimumThresholdForBaking()
    {
        var inputReason = new Application.NodeApi.StakeUnderMinimumThresholdForBaking();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.StakeUnderMinimumThresholdForBaking>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_BakerInCooldown()
    {
        var inputReason = new Application.NodeApi.BakerInCooldown();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.BakerInCooldown>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_DuplicateAggregationKey()
    {
        var inputReason = new Application.NodeApi.DuplicateAggregationKey("98528ef89dc117f102ef3f089c81b92e4d945d22c0269269af6ef9f876d79e828b31b8b4b8cc3d9234c30e83bd79e20a0a807bc110f0ac9babae90cb6a8c6d0deb2e5627704b41bdd646a547895fd1f9f2a7b0dd4fb4e138356e91d002a28f83");
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DuplicateAggregationKey>();
        result.AggregationKey.Should().Be("98528ef89dc117f102ef3f089c81b92e4d945d22c0269269af6ef9f876d79e828b31b8b4b8cc3d9234c30e83bd79e20a0a807bc110f0ac9babae90cb6a8c6d0deb2e5627704b41bdd646a547895fd1f9f2a7b0dd4fb4e138356e91d002a28f83");
    }

    [Fact]
    public async Task TransactionRejectReason_NonExistentCredentialId()
    {
        var inputReason = new Application.NodeApi.NonExistentCredentialId();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NonExistentCredentialId>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_KeyIndexAlreadyInUse()
    {
        var inputReason = new Application.NodeApi.KeyIndexAlreadyInUse();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.KeyIndexAlreadyInUse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidAccountThreshold()
    {
        var inputReason = new Application.NodeApi.InvalidAccountThreshold();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidAccountThreshold>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidCredentialKeySignThreshold()
    {
        var inputReason = new Application.NodeApi.InvalidCredentialKeySignThreshold();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidCredentialKeySignThreshold>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidEncryptedAmountTransferProof()
    {
        var inputReason = new Application.NodeApi.InvalidEncryptedAmountTransferProof();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidEncryptedAmountTransferProof>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidTransferToPublicProof()
    {
        var inputReason = new Application.NodeApi.InvalidTransferToPublicProof();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidTransferToPublicProof>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_EncryptedAmountSelfTransfer()
    {
        var inputReason = new Application.NodeApi.EncryptedAmountSelfTransfer(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.EncryptedAmountSelfTransfer>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidIndexOnEncryptedTransfer()
    {
        var inputReason = new Application.NodeApi.InvalidIndexOnEncryptedTransfer();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidIndexOnEncryptedTransfer>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_ZeroScheduledAmount()
    {
        var inputReason = new Application.NodeApi.ZeroScheduledAmount();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ZeroScheduledAmount>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NonIncreasingSchedule()
    {
        var inputReason = new Application.NodeApi.NonIncreasingSchedule();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NonIncreasingSchedule>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_FirstScheduledReleaseExpired()
    {
        var inputReason = new Application.NodeApi.FirstScheduledReleaseExpired();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.FirstScheduledReleaseExpired>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_ScheduledSelfTransfer()
    {
        var inputReason = new Application.NodeApi.ScheduledSelfTransfer(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.ScheduledSelfTransfer>();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionRejectReason_InvalidCredentials()
    {
        var inputReason = new Application.NodeApi.InvalidCredentials();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InvalidCredentials>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_DuplicateCredIds()
    {
        var inputReason = new Application.NodeApi.DuplicateCredIds(new[] { "b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f" });
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DuplicateCredIds>();
        result.CredIds.Should().ContainSingle().Which.Should().Be("b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f");
    }

    [Fact]
    public async Task TransactionRejectReason_NonExistentCredIds()
    {
        var inputReason = new Application.NodeApi.NonExistentCredIds(new[] { "b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f" });
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NonExistentCredIds>();
        result.CredIds.Should().ContainSingle().Which.Should().Be("b9a35cfb9556b897d3c1e81ab8247e916762755a7673bd493a2062a6988033e6a37d88c366a89109fa6e26ba7a317b7f");
    }

    [Fact]
    public async Task TransactionRejectReason_RemoveFirstCredential()
    {
        var inputReason = new Application.NodeApi.RemoveFirstCredential();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.RemoveFirstCredential>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_CredentialHolderDidNotSign()
    {
        var inputReason = new Application.NodeApi.CredentialHolderDidNotSign();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.CredentialHolderDidNotSign>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NotAllowedMultipleCredentials()
    {
        var inputReason = new Application.NodeApi.NotAllowedMultipleCredentials();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotAllowedMultipleCredentials>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NotAllowedToReceiveEncrypted()
    {
        var inputReason = new Application.NodeApi.NotAllowedToReceiveEncrypted();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotAllowedToReceiveEncrypted>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionRejectReason_NotAllowedToHandleEncrypted()
    {
        var inputReason = new Application.NodeApi.NotAllowedToHandleEncrypted();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotAllowedToHandleEncrypted>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_MissingBakerAddParameters()
    {
        var inputReason = new Application.NodeApi.MissingBakerAddParameters();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.MissingBakerAddParameters>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_FinalizationRewardCommissionNotInRange()
    {
        var inputReason = new Application.NodeApi.FinalizationRewardCommissionNotInRange();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.FinalizationRewardCommissionNotInRange>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_BakingRewardCommissionNotInRange()
    {
        var inputReason = new Application.NodeApi.BakingRewardCommissionNotInRange();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.BakingRewardCommissionNotInRange>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_TransactionFeeCommissionNotInRange()
    {
        var inputReason = new Application.NodeApi.TransactionFeeCommissionNotInRange();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.TransactionFeeCommissionNotInRange>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_AlreadyADelegator()
    {
        var inputReason = new Application.NodeApi.AlreadyADelegator();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.AlreadyADelegator>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_InsufficientBalanceForDelegationStake()
    {
        var inputReason = new Application.NodeApi.InsufficientBalanceForDelegationStake();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InsufficientBalanceForDelegationStake>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_MissingDelegationAddParameters()
    {
        var inputReason = new Application.NodeApi.MissingDelegationAddParameters();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<MissingDelegationAddParameters>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_InsufficientDelegationStake()
    {
        var inputReason = new Application.NodeApi.InsufficientDelegationStake();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.InsufficientDelegationStake>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_DelegatorInCooldown()
    {
        var inputReason = new Application.NodeApi.DelegatorInCooldown();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DelegatorInCooldown>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_NotADelegator()
    {
        var inputReason = new Application.NodeApi.NotADelegator(AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.NotADelegator>();
        result.Should().NotBeNull();
        result.AccountAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }
    
    [Fact]
    public async Task TransactionRejectReason_DelegationTargetNotABaker()
    {
        var inputReason = new Application.NodeApi.DelegationTargetNotABaker(42UL);
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.DelegationTargetNotABaker>();
        result.Should().NotBeNull();
        result.BakerId.Should().Be(42UL);
    }
    
    [Fact]
    public async Task TransactionRejectReason_StakeOverMaximumThresholdForPool()
    {
        var inputReason = new Application.NodeApi.StakeOverMaximumThresholdForPool();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.StakeOverMaximumThresholdForPool>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_PoolWouldBecomeOverDelegated()
    {
        var inputReason = new Application.NodeApi.PoolWouldBecomeOverDelegated();
        await WriteSingleRejectedTransaction(inputReason);
        
        var result = await ReadSingleRejectedTransactionRejectReason<Application.Api.GraphQL.Transactions.PoolWouldBecomeOverDelegated>();
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TransactionRejectReason_PoolClosed()
    {
        var inputReason = new Application.NodeApi.PoolClosed();
        await WriteSingleRejectedTransaction(inputReason);
        
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
    
    private async Task WriteSingleRejectedTransaction(Application.NodeApi.TransactionRejectReason rejectReason)
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionRejectResultBuilder()
                    .WithRejectReason(rejectReason)
                    .Build())
                .Build());

        await WriteData();
    }
    
    private async Task<T> ReadSingleRejectedTransactionRejectReason<T>() where T : TransactionRejectReason
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = await dbContext.Transactions.SingleAsync();
        var rejected = Assert.IsType<Rejected>(transaction.Result);
        return Assert.IsType<T>(rejected.Reason);
    }
}