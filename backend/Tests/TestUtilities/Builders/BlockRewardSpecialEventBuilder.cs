using Application.NodeApi;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class BlockRewardSpecialEventBuilder
{
    private CcdAmount _bakerReward = CcdAmount.FromMicroCcd(5111884);
    private CcdAmount _foundationCharge = CcdAmount.FromMicroCcd(4884);
    private CcdAmount _transactionFees = CcdAmount.FromMicroCcd(8888);
    private CcdAmount _newGasAccount = CcdAmount.FromMicroCcd(455);
    private CcdAmount _oldGasAccount = CcdAmount.FromMicroCcd(22);
    private AccountAddress _baker = AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    private AccountAddress _foundationAccount = AccountAddress.From("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi");

    public BlockRewardSpecialEvent Build()
    {
        return new BlockRewardSpecialEvent()
        {
            BakerReward = _bakerReward,
            FoundationCharge = _foundationCharge,
            TransactionFees = _transactionFees,
            NewGasAccount = _newGasAccount,
            OldGasAccount = _oldGasAccount,
            Baker = _baker,
            FoundationAccount = _foundationAccount
        };
    }

    public BlockRewardSpecialEventBuilder WithBakerReward(CcdAmount value)
    {
        _bakerReward = value;
        return this;
    }

    public BlockRewardSpecialEventBuilder WithFoundationCharge(CcdAmount value)
    {
        _foundationCharge = value;
        return this;
    }

    public BlockRewardSpecialEventBuilder WithTransactionFees(CcdAmount value)
    {
        _transactionFees = value;
        return this;
    }

    public BlockRewardSpecialEventBuilder WithNewGasAccount(CcdAmount value)
    {
        _newGasAccount = value;
        return this;
    }

    public BlockRewardSpecialEventBuilder WithOldGasAccount(CcdAmount value)
    {
        _oldGasAccount = value;
        return this;
    }

    public BlockRewardSpecialEventBuilder WithBaker(AccountAddress value)
    {
        _baker = value;
        return this;
    }

    public BlockRewardSpecialEventBuilder WithFoundationAccount(AccountAddress value)
    {
        _foundationAccount = value;
        return this;
    }
}