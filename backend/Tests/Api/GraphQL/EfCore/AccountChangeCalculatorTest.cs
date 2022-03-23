using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi.Types;
using FluentAssertions;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;

namespace Tests.Api.GraphQL.EfCore;

public class AccountChangeCalculatorTest
{
    private readonly AccountChangeCalculator _target;
    private readonly AccountTransactionRelation[] _noTransactions = Array.Empty<AccountTransactionRelation>();
    private readonly AccountBalanceUpdate[] _noBalanceUpdates = Array.Empty<AccountBalanceUpdate>();
    private readonly AccountLookupStub _accountLookupStub;

    public AccountChangeCalculatorTest()
    {
        _accountLookupStub = new AccountLookupStub();
        _target = new AccountChangeCalculator(_accountLookupStub);
    }

    [Fact]
    public async Task CreateAggregatedAccountUpdates_NoUpdates()
    {
        var result = await _target.CreateAggregatedAccountUpdates(_noBalanceUpdates, _noTransactions);
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task CreateAggregatedAccountUpdates_AmountAdjustment_SingleUpdate()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 10);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(100).Build()
        };
        
        var result = await _target.CreateAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(10, 100, 0)
        });
    }

    [Fact]
    public async Task CreateAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToSameAccountWithSameAddress()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 10);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(-800).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(300).Build(),
        };
        
        var result = await _target.CreateAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(10, -400, 0)
        });
    }
    
    [Fact] 
    public async Task CreateAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToSameAccountWithAliasAddresses()
    {
        var accountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        _accountLookupStub.AddToCache(accountAddress.GetBaseAddress().AsString, 10);

        var balanceUpdates = new []
        {
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress.CreateAliasAddress(10, 201, 8)).WithAmountAdjustment(100).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress).WithAmountAdjustment(-800).Build(),
            new AccountBalanceUpdateBuilder().WithAccountAddress(accountAddress.CreateAliasAddress(10, 201, 8)).WithAmountAdjustment(300).Build(),
        };
        
        var result = await _target.CreateAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(10, -400, 0)
        });
    }

    [Fact] 
    public async Task CreateAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToMultipleAccounts()
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
        
        var result = await _target.CreateAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(10, -700, 0),
            new AccountWriter.AccountUpdate(11, 550, 0)
        });
    }
    
    [Fact] 
    public async Task CreateAggregatedAccountUpdates_AmountAdjustment_MultipleUpdatesToMultipleAccounts_RemoveResultsThatWouldLeadToNoChanges()
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
        
        var result = await _target.CreateAggregatedAccountUpdates(balanceUpdates, _noTransactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(11, 50, 0)
        });
    }
    
    [Fact] 
    public async Task CreateAggregatedAccountUpdates_TransactionsAdded_SingleAccountSingleTransaction()
    {
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build()
        };
        var result = await _target.CreateAggregatedAccountUpdates(_noBalanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(11, 0, 1)
        });
    }

    [Fact] 
    public async Task CreateAggregatedAccountUpdates_TransactionsAdded_SingleAccountMultipleTransactions()
    {
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build()
        };
        var result = await _target.CreateAggregatedAccountUpdates(_noBalanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(11, 0, 3)
        });
    }
    
    [Fact] 
    public async Task CreateAggregatedAccountUpdates_TransactionsAdded_MultipleAccountsMultipleTransactions()
    {
        var transactions = new[]
        {
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(42).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(11).Build(),
            new AccountTransactionRelationBuilder().WithAccountId(42).Build()
        };
        var result = await _target.CreateAggregatedAccountUpdates(_noBalanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(11, 0, 3),
            new AccountWriter.AccountUpdate(42, 0, 2)
        });
    }
    
    [Fact] 
    public async Task CreateAggregatedAccountUpdates_ResultsFlattened()
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
        var result = await _target.CreateAggregatedAccountUpdates(balanceUpdates, transactions);
        
        result.Should().BeEquivalentTo(new []
        {
            new AccountWriter.AccountUpdate(11, 400, 2),
        });
    }
}