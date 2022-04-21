using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class SpecialEventJsonConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public SpecialEventJsonConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void Deserialize_Mint()
    {
        var json = "{\"tag\": \"Mint\", \"mintBakingReward\": \"31752785\", \"foundationAccount\": \"3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi\", \"mintFinalizationReward\": \"15876392\", \"mintPlatformDevelopmentCharge\": \"5292132\"}";
        var result = JsonSerializer.Deserialize<SpecialEvent>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<MintSpecialEvent>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(31752785), typed.MintBakingReward);
        Assert.Equal(CcdAmount.FromMicroCcd(15876392), typed.MintFinalizationReward);
        Assert.Equal(CcdAmount.FromMicroCcd(5292132), typed.MintPlatformDevelopmentCharge);
        Assert.Equal(new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi"), typed.FoundationAccount);
    }

    [Fact]
    public void Deserialize_BlockReward()
    {
        var json = "{\"tag\": \"BlockReward\", \"baker\": \"44Hva7dk3pZfw2E5CDw7sevZvMbUgTYLrYQMxTpPZtjzukUYjB\", \"bakerReward\": \"51425\", \"newGASAccount\": \"5125\", \"oldGASAccount\": \"141\", \"transactionFees\": \"584\", \"foundationCharge\": \"678955\", \"foundationAccount\": \"3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi\"}";
        var result = JsonSerializer.Deserialize<SpecialEvent>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<BlockRewardSpecialEvent>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(51425), typed.BakerReward);
        Assert.Equal(CcdAmount.FromMicroCcd(5125), typed.NewGasAccount);
        Assert.Equal(CcdAmount.FromMicroCcd(141), typed.OldGasAccount);
        Assert.Equal(CcdAmount.FromMicroCcd(584), typed.TransactionFees);
        Assert.Equal(CcdAmount.FromMicroCcd(678955), typed.FoundationCharge);
        Assert.Equal(new AccountAddress("44Hva7dk3pZfw2E5CDw7sevZvMbUgTYLrYQMxTpPZtjzukUYjB"), typed.Baker);
        Assert.Equal(new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi"), typed.FoundationAccount);
    }
    
    [Fact]
    public void Deserialize_FinalizationReward()
    {
        var json = "{\"tag\": \"FinalizationRewards\", \"remainder\": \"2\", \"finalizationRewards\": [{\"amount\": \"1587639\", \"address\": \"38rQoCqvUkfVQ1fTwVPLBgjLkkZ8x79HozGYsWsZmtCyipiMnp\"}, {\"amount\": \"1587639\", \"address\": \"3KoNZL5xiFNpCyAvQnAZKNyB7NSxjZtBUdmoZhHXpHreY2Fvb4\"}, {\"amount\": \"1587639\", \"address\": \"3Ug5rCqAN2z17MqAyh5KUGDpv6k9eSHu8AN8jCgbmAxmjcu5TM\"}, {\"amount\": \"1587639\", \"address\": \"3WSCvbSWi2fi89htQzzPqc9aoK9gBUdSJHy8veYQNLdhyUCXWh\"}, {\"amount\": \"1587639\", \"address\": \"3fYHckLWcVDp7Ut7TPxdtieXwpSZgehnp2Hs9Pv2JewrCkxTqZ\"}, {\"amount\": \"1587639\", \"address\": \"44Hva7dk3pZfw2E5CDw7sevZvMbUgTYLrYQMxTpPZtjzukUYjB\"}, {\"amount\": \"1587639\", \"address\": \"46hs9HFuYEt5Gq8bKn7ZMpMUUYvTMbfqfqUtPJqqQF3mVFGKeH\"}, {\"amount\": \"1587639\", \"address\": \"4Gaw3Y44fyGzaNbG69eZyr1Q5fByMvSuQ5pKRW7xRmDzajKtMS\"}, {\"amount\": \"1587639\", \"address\": \"4hXCdgNTxgM7LNm8nFJEfjDhEcyjjqQnPSRyBS9QgmHKQVxKRf\"}, {\"amount\": \"1587639\", \"address\": \"4mExBH7B4D2fzC2NGD7KQVienkMsXeNBpie7Nba5HfrLpwCKWp\"}]}";
        var result = JsonSerializer.Deserialize<SpecialEvent>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<FinalizationRewardsSpecialEvent>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(2), typed.Remainder);
        Assert.Equal(10, typed.FinalizationRewards.Length);
        Assert.Equal(CcdAmount.FromMicroCcd(1587639), typed.FinalizationRewards[0].Amount);
        Assert.Equal(new AccountAddress("38rQoCqvUkfVQ1fTwVPLBgjLkkZ8x79HozGYsWsZmtCyipiMnp"), typed.FinalizationRewards[0].Address);
        Assert.Equal(CcdAmount.FromMicroCcd(1587639), typed.FinalizationRewards[1].Amount);
        Assert.Equal(new AccountAddress("3KoNZL5xiFNpCyAvQnAZKNyB7NSxjZtBUdmoZhHXpHreY2Fvb4"), typed.FinalizationRewards[1].Address);
    }

    [Fact]
    public void Deserialize_BakingRewards()
    {
        var json = "{\"tag\": \"BakingRewards\", \"remainder\": \"11\", \"bakerRewards\": [{\"amount\": \"628516302043\", \"address\": \"38rQoCqvUkfVQ1fTwVPLBgjLkkZ8x79HozGYsWsZmtCyipiMnp\"}, {\"amount\": \"179576086298\", \"address\": \"3KoNZL5xiFNpCyAvQnAZKNyB7NSxjZtBUdmoZhHXpHreY2Fvb4\"}, {\"amount\": \"89788043149\", \"address\": \"3Ug5rCqAN2z17MqAyh5KUGDpv6k9eSHu8AN8jCgbmAxmjcu5TM\"}, {\"amount\": \"538728258894\", \"address\": \"3WSCvbSWi2fi89htQzzPqc9aoK9gBUdSJHy8veYQNLdhyUCXWh\"}, {\"amount\": \"179576086298\", \"address\": \"3fYHckLWcVDp7Ut7TPxdtieXwpSZgehnp2Hs9Pv2JewrCkxTqZ\"}, {\"amount\": \"448940215745\", \"address\": \"44Hva7dk3pZfw2E5CDw7sevZvMbUgTYLrYQMxTpPZtjzukUYjB\"}, {\"amount\": \"538728258894\", \"address\": \"46hs9HFuYEt5Gq8bKn7ZMpMUUYvTMbfqfqUtPJqqQF3mVFGKeH\"}, {\"amount\": \"359152172596\", \"address\": \"4Gaw3Y44fyGzaNbG69eZyr1Q5fByMvSuQ5pKRW7xRmDzajKtMS\"}, {\"amount\": \"448940215745\", \"address\": \"4hXCdgNTxgM7LNm8nFJEfjDhEcyjjqQnPSRyBS9QgmHKQVxKRf\"}, {\"amount\": \"179576086298\", \"address\": \"4mExBH7B4D2fzC2NGD7KQVienkMsXeNBpie7Nba5HfrLpwCKWp\"}]}";
        var result = JsonSerializer.Deserialize<SpecialEvent>(json, _serializerOptions);
        
        Assert.NotNull(result);
        var typed = Assert.IsType<BakingRewardsSpecialEvent>(result);
        Assert.Equal(CcdAmount.FromMicroCcd(11), typed.Remainder);
        Assert.Equal(10, typed.BakerRewards.Length);
        Assert.Equal(CcdAmount.FromMicroCcd(628516302043), typed.BakerRewards[0].Amount);
        Assert.Equal(new AccountAddress("38rQoCqvUkfVQ1fTwVPLBgjLkkZ8x79HozGYsWsZmtCyipiMnp"), typed.BakerRewards[0].Address);
        Assert.Equal(CcdAmount.FromMicroCcd(179576086298), typed.BakerRewards[1].Amount);
        Assert.Equal(new AccountAddress("3KoNZL5xiFNpCyAvQnAZKNyB7NSxjZtBUdmoZhHXpHreY2Fvb4"), typed.BakerRewards[1].Address);
    }

    [Fact]
    public void Deserialize_UnknownSpecialEvent()
    {
        var json = "{\"tag\": \"FooBar\", \"remainder\": \"42\" }";
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<SpecialEvent>(json, _serializerOptions));
    }
}