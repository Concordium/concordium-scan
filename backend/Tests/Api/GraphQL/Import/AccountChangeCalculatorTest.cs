using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using FluentAssertions;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;
using AccountCreated = ConcordiumSdk.NodeApi.Types.AccountCreated;
using CredentialDeployed = ConcordiumSdk.NodeApi.Types.CredentialDeployed;
using TimestampedAmount = ConcordiumSdk.NodeApi.Types.TimestampedAmount;
using Transferred = ConcordiumSdk.NodeApi.Types.Transferred;
using TransferredWithSchedule = ConcordiumSdk.NodeApi.Types.TransferredWithSchedule;

namespace Tests.Api.GraphQL.Import;

public class AccountChangeCalculatorTest
{
    private readonly AccountChangeCalculator _target;
    private readonly AccountTransactionRelation[] _noTransactions = Array.Empty<AccountTransactionRelation>();
    private readonly AccountBalanceUpdate[] _noBalanceUpdates = Array.Empty<AccountBalanceUpdate>();
    private readonly AccountLookupStub _accountLookupStub;

    public AccountChangeCalculatorTest()
    {
        _accountLookupStub = new AccountLookupStub();
        _target = new AccountChangeCalculator(_accountLookupStub, new NullMetrics());
    }

    [Fact]
    public void GetAggregatedAccountUpdates_NoUpdates()
    {
        var result = _target.GetAggregatedAccountUpdates(_noBalanceUpdates, _noTransactions);
        result.Should().BeEmpty();
    }
    
    [Fact]
    public void GetAggregatedAccountUpdates_AmountAdjustment_SingleUpdate()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 10);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(100).Build()
        };
        
        var result = _target.GetAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(10, 100, 0)
        });
    }

    [Fact]
    public void GetAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToSameAccountWithSameAddress()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 10);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(-800).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(300).Build(),
        };
        
        var result = _target.GetAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(10, -400, 0)
        });
    }
    
    [Fact] 
    public void GetAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToSameAccountWithAliasAddresses()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 10);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress.CreateAliasAddress(10, 201, 8)).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(-800).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress.CreateAliasAddress(10, 201, 8)).WithAmountAdjustment(300).Build(),
        };
        
        var result = _target.GetAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(10, -400, 0)
        });
    }

    [Fact] 
    public void GetAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToMultipleAccounts()
    {
        var accountAddress1 = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        var accountAddress2 = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(accountAddress1.GetBaseAddress().AsString, 10);   
        _accountLookupStub.AddToCache(accountAddress2.GetBaseAddress().AsString, 11);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress1).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress1.CreateAliasAddress(2, 10, 127)).WithAmountAdjustment(-800).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress2).WithAmountAdjustment(250).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress2.CreateAliasAddress(10, 201, 8)).WithAmountAdjustment(300).Build(),
        };
        
        var result = _target.GetAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(10, -700, 0),
            new AccountUpdate(11, 550, 0)
        });
    }
    
    [Fact] 
    public void GetAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToMultipleAccounts_KeepResultsThatWouldLeadToNoChanges()
    {
        var accountAddress1 = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        var accountAddress2 = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        var accountAddress3 = new AccountAddress("3FYcaWUucnbXxvtnQQC5zpK91oN67MDbTiwzKzQUkVirKDrRce");
        _accountLookupStub.AddToCache(accountAddress1.GetBaseAddress().AsString, 10);   
        _accountLookupStub.AddToCache(accountAddress2.GetBaseAddress().AsString, 11);   
        _accountLookupStub.AddToCache(accountAddress3.GetBaseAddress().AsString, 12);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress1).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress1.CreateAliasAddress(2, 10, 127)).WithAmountAdjustment(-100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress2).WithAmountAdjustment(50).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress3).WithAmountAdjustment(0).Build(),
        };
        
        var result = _target.GetAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(10, 0, 0),
            new AccountUpdate(11, 50, 0),
            new AccountUpdate(12, 0, 0)
        });
    }
    
    [Fact] 
    public void GetAggregatedAccountUpdates_TransactionsAdded_SingleAccountSingleTransaction()
    {
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build()
        };
        var result = _target.GetAggregatedAccountUpdates(_noBalanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(11, 0, 1)
        });
    }

    [Fact] 
    public void GetAggregatedAccountUpdates_TransactionsAdded_SingleAccountMultipleTransactions()
    {
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build()
        };
        var result = _target.GetAggregatedAccountUpdates(_noBalanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(11, 0, 3)
        });
    }
    
    [Fact] 
    public void GetAggregatedAccountUpdates_TransactionsAdded_MultipleAccountsMultipleTransactions()
    {
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(42).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(42).Build()
        };
        var result = _target.GetAggregatedAccountUpdates(_noBalanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(11, 0, 3),
            new AccountUpdate(42, 0, 2)
        });
    }
    
    [Fact] 
    public void GetAggregatedAccountUpdates_ResultsFlattened()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 11);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(300).Build(),
        };
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
        };
        var result = _target.GetAggregatedAccountUpdates(balanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountUpdate(11, 400, 2),
        });
    }
    
    
    [Fact] 
    public void GetAccountTransactionRelations_AccountExists_SingleTransactionWithSameAddressTwice()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, 13);

        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(null)
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new AccountCreated(canonicalAddress),
                        new CredentialDeployed("1234", canonicalAddress))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        var result = _target.GetAccountTransactionRelations(new[] { input });

        result.Length.Should().Be(1);
        result[0].AccountId.Should().Be(13);
        result[0].TransactionId.Should().Be(42);
    }
    
    [Fact]
    public void GetAccountTransactionRelations_AccountExists_MultipleTransactionsWithSameAddress()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, 15);

        var input1 = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(canonicalAddress)
                .Build(),
            new Transaction { Id = 42 });
        var input2 = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(canonicalAddress)
                .Build(),
            new Transaction { Id = 43 });

        var result = _target.GetAccountTransactionRelations(new[] { input1, input2 });
        result.Length.Should().Be(2);
        result[0].AccountId.Should().Be(15);
        result[0].TransactionId.Should().Be(42);
        result[1].AccountId.Should().Be(15);
        result[1].TransactionId.Should().Be(43);
    }
    
    /// <summary>
    /// Some account addresses found in the hierarchy might not exist (example: some reject reasons will include non-existing addresses).
    /// Therefore we will simply avoid creating relations for these addresses.
    /// </summary>
    [Fact]
    public void GetAccountTransactionRelations_AccountDoesNotExist()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, null);

        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(null)
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new AccountCreated(canonicalAddress),
                        new CredentialDeployed("1234", canonicalAddress))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        var returnedResult = _target.GetAccountTransactionRelations(new[] { input });
        returnedResult.Should().BeEmpty();
    }
    
    [Fact]
    public void GetAccountTransactionRelations_AccountExists_SingleTransactionWithAnAliasAddress()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        var aliasAddress = canonicalAddress.CreateAliasAddress(48, 11, 99);
        
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, 15);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(canonicalAddress)
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Transferred(CcdAmount.FromCcd(10), canonicalAddress, aliasAddress))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        var result = _target.GetAccountTransactionRelations(new[] { input });
        result.Length.Should().Be(1);
        result[0].AccountId.Should().Be(15);
        result[0].TransactionId.Should().Be(42);
    }
    
    [Fact]
    public void GetAccountReleaseScheduleItems_AccountsExists_CanonicalAddress()
    {
        var toCanonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(toCanonicalAddress.GetBaseAddress().AsString, 13);
        
        var fromCanonicalAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(fromCanonicalAddress.GetBaseAddress().AsString, 14);

        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        fromCanonicalAddress, 
                        toCanonicalAddress,
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        var result = _target.GetAccountReleaseScheduleItems(new []{ input });

        result.Length.Should().Be(2);
        AssertEqual(result[0], 13, 42, 0, baseTime.AddHours(1), 515151, 14);
        AssertEqual(result[1], 13, 42, 1, baseTime.AddHours(2), 4242, 14);
    }

    [Fact]
    public void GetAccountReleaseScheduleItems_AccountsExists_AliasAddresses()
    {
        var toCanonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(toCanonicalAddress.GetBaseAddress().AsString, 10);
        var toAliasAddress = toCanonicalAddress.CreateAliasAddress(38, 11, 200);
        
        var fromCanonicalAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(fromCanonicalAddress.GetBaseAddress().AsString, 27);
        var fromAliasAddress = fromCanonicalAddress.CreateAliasAddress(10, 79, 5);

        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        fromAliasAddress, 
                        toAliasAddress,
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        var result = _target.GetAccountReleaseScheduleItems(new []{ input });

        result.Length.Should().Be(2);
        AssertEqual(result[0], 10, 42, 0, baseTime.AddHours(1), 515151, 27);
        AssertEqual(result[1], 10, 42, 1, baseTime.AddHours(2), 4242, 27);
    }

    [Fact]
    public void GetAccountReleaseScheduleItems_AccountDoesntExist()
    {
        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"), 
                        new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        // We do not ever expect a scheduled transfer to complete successfully if either sender or receiver does not exist!
        Assert.ThrowsAny<Exception>(() => _target.GetAccountReleaseScheduleItems(new []{ input }));
    }

    private static void AssertEqual(AccountReleaseScheduleItem actual, long expectedAccountId, int expectedTransactionId, int expectedScheduleIndex, DateTimeOffset expectedTimestamp, ulong expectedAmount, long expectedFromAccountId)
    {
        Assert.Equal(expectedAccountId, actual.AccountId);
        Assert.Equal(expectedTransactionId, actual.TransactionId);
        Assert.Equal(expectedScheduleIndex, actual.Index);
        Assert.Equal(expectedTimestamp, actual.Timestamp);
        Assert.Equal(expectedAmount, actual.Amount);
        Assert.Equal(expectedFromAccountId, actual.FromAccountId);
    }
}