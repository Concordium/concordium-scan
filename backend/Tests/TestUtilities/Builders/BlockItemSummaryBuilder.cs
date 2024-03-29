using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class UpdateDetailsBuilder
{
    private readonly IUpdatePayload _payload;
    private DateTimeOffset _effectiveTime = DateTimeOffset.MinValue;

    public UpdateDetailsBuilder(IUpdatePayload payload)
    {
        _payload = payload;
    }

    public UpdateDetails Build() =>
        new(
            _effectiveTime, _payload
        );

    public UpdateDetailsBuilder WithEffectiveTime(DateTimeOffset effectiveTime)
    {
        _effectiveTime = effectiveTime;
        return this;
    }
}

public class AccountCreationDetailsBuilder
{
    private readonly CredentialType _credentialType;
    private AccountAddress _accountAddress = AccountAddress.From(new byte[32]);
    private CredentialRegistrationId _credentialRegistrationId = new(
        Array.Empty<byte>());
    
    public AccountCreationDetailsBuilder(CredentialType credentialType)
    {
        _credentialType = credentialType;
    }
    
    public AccountCreationDetails Build()
    {
        return new AccountCreationDetails(_credentialType, _accountAddress, _credentialRegistrationId);
    }

    public AccountCreationDetailsBuilder WithAccountAddress(AccountAddress accountAddress)
    {
        _accountAddress = accountAddress;
        return this;
    }

    public AccountCreationDetailsBuilder WithCredentialRegistrationId(CredentialRegistrationId credentialRegistrationId)
    {
        _credentialRegistrationId = credentialRegistrationId;
        return this;
    }
}

public class AccountTransactionDetailsBuilder
{
    private CcdAmount _cost = CcdAmount.FromCcd(10);
    private AccountAddress _sender = AccountAddress.From(new byte[32]);
    private IAccountTransactionEffects _accountTransactionEffects;

    public AccountTransactionDetailsBuilder(IAccountTransactionEffects accountTransactionEffects)
    {
        _accountTransactionEffects = accountTransactionEffects;
    }

    public AccountTransactionDetails Build()
    {
        return new AccountTransactionDetails(_cost, _sender, _accountTransactionEffects);
    }

    public AccountTransactionDetailsBuilder WithCost(CcdAmount cost)
    {
        _cost = cost;
        return this;
    }

    public AccountTransactionDetailsBuilder WithSender(AccountAddress sender)
    {
        _sender = sender;
        return this;
    }

    public AccountTransactionDetailsBuilder WithAccountTransactionEffects(IAccountTransactionEffects effects)
    {
        _accountTransactionEffects = effects;
        return this;
    }
}

public class BlockItemSummaryBuilder
{
    private ulong _index = 0;
    private EnergyAmount _energyAmount = new EnergyAmount(100);
    private TransactionHash _transactionHash = TransactionHash.From("42B83D2BE10B86BD6DF5C102C4451439422471BC4443984912A832052FF7485B");
    private readonly IBlockItemSummaryDetails _details;

    public BlockItemSummaryBuilder(IBlockItemSummaryDetails details)
    {
        _details = details;
    }

    public BlockItemSummary Build()
    {
        return new BlockItemSummary(
            _index,
            _energyAmount,
            _transactionHash,
            _details
        );
    }
    
    public BlockItemSummaryBuilder WithIndex(ulong index)
    {
        _index = index;
        return this;
    }
    
    public BlockItemSummaryBuilder WithEnergyAmount(EnergyAmount energyAmount)
    {
        _energyAmount = energyAmount;
        return this;
    }
    
    public BlockItemSummaryBuilder WithTransactionHash(TransactionHash hash)
    {
        _transactionHash = hash;
        return this;
    }
}