using System.Text.Json;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TransactionResultEventConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionResultEventConverterTest()
    {
        _serializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    [Fact]
    public void RoundTrip_ModuleDeployed()
    {
        var json = "{\"tag\": \"ModuleDeployed\", \"contents\": \"cd1f24bab3dc05199a9052a47d782e020d1445fa4c974fc3a51802eb32aa6983\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<ModuleDeployed>(deserialized);
        typed.Contents.Should().Be(new ModuleRef("cd1f24bab3dc05199a9052a47d782e020d1445fa4c974fc3a51802eb32aa6983"));

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_ContractInitialized()
    {
        var json = "{\"ref\": \"2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb\", \"tag\": \"ContractInitialized\", \"amount\": \"1578\", \"events\": [\"fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4\", \"fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00\"], \"address\": {\"index\": 1423, \"subindex\": 1}, \"initName\": \"init_CIS1-singleNFT\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<ContractInitialized>(deserialized);
        typed.Ref.Should().Be(new ModuleRef("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"));
        typed.Amount.Should().Be(CcdAmount.FromMicroCcd(1578));
        typed.Events.Should().Equal(
            BinaryData.FromHexString("fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4"),
            BinaryData.FromHexString("fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00"));
        typed.Address.Should().Be(new ContractAddress(1423, 1));
        typed.InitName.Should().Be("init_CIS1-singleNFT");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_Updated()
    {
        var json = "{\"tag\": \"Updated\", \"amount\": \"20\", \"events\": [\"05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32\", \"01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32\"], \"address\": {\"index\": 35, \"subindex\": 0}, \"message\": \"080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32\", \"instigator\": {\"type\": \"AddressContract\", \"address\": {\"index\": 37, \"subindex\": 0}}, \"receiveName\": \"inventory.transfer\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<Updated>(deserialized);
        typed.Address.Should().Be(new ContractAddress(35, 0));
        typed.Instigator.Should().Be(new ContractAddress(37, 0));
        typed.Amount.Should().Be(CcdAmount.FromMicroCcd(20));
        typed.Message.Should().Be(BinaryData.FromHexString("080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"));
        typed.ReceiveName.Should().Be("inventory.transfer");
        typed.Events.Should().Equal(
            BinaryData.FromHexString("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"),
            BinaryData.FromHexString("01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_Transferred()
    {
        var json = "{\"to\": {\"type\": \"AddressAccount\", \"address\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\"}, \"tag\": \"Transferred\", \"from\": {\"type\": \"AddressContract\", \"address\": {\"index\": 858, \"subindex\": 42}}, \"amount\": \"500000000\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<Transferred>(deserialized);

        typed.To.Should().Be(new AccountAddress("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN"));
        typed.From.Should().Be(new ContractAddress(858, 42));
        typed.Amount.Should().Be(CcdAmount.FromMicroCcd(500000000));

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_AccountCreated()
    {
        var json = "{\"tag\": \"AccountCreated\", \"contents\": \"3aTTghVWSQPRKEXhE5a4aUWsvSeNEMHYFa25sgxAP3HZVeU25p\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<AccountCreated>(deserialized);
        Assert.Equal("3aTTghVWSQPRKEXhE5a4aUWsvSeNEMHYFa25sgxAP3HZVeU25p", typed.Contents.AsString);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_CredentialDeployed()
    {
        var json = "{\"tag\": \"CredentialDeployed\", \"regId\": \"b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d\", \"account\": \"3aTTghVWSQPRKEXhE5a4aUWsvSeNEMHYFa25sgxAP3HZVeU25p\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<CredentialDeployed>(deserialized);
        Assert.Equal("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d", typed.RegId);
        Assert.Equal("3aTTghVWSQPRKEXhE5a4aUWsvSeNEMHYFa25sgxAP3HZVeU25p", typed.Account.AsString);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_BakerAdded()
    {
        var json = "{\"tag\": \"BakerAdded\", \"stake\": \"10033873900000\", \"account\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\", \"bakerId\": 98, \"signKey\": \"418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c\", \"electionKey\": \"dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88\", \"aggregationKey\": \"823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1\", \"restakeEarnings\": true}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerAdded>(deserialized);
        Assert.Equal<ulong>(10033873900000, typed.Stake.MicroCcdValue);
        Assert.Equal("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN", typed.Account.AsString);
        Assert.Equal<ulong>(98, typed.BakerId);
        Assert.Equal("418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c", typed.SignKey);
        Assert.Equal("dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88", typed.ElectionKey);
        Assert.Equal("823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1", typed.AggregationKey);
        Assert.True(typed.RestakeEarnings);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_BakerRemoved()
    {
        var json = "{\"tag\": \"BakerRemoved\", \"account\": \"3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH\", \"bakerId\": 41}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerRemoved>(deserialized);
        Assert.Equal("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH", typed.Account.AsString);
        Assert.Equal<ulong>(41, typed.BakerId);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_BakerStakeIncreased()
    {
        var json = "{\"tag\": \"BakerStakeIncreased\", \"account\": \"3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe\", \"bakerId\": 26, \"newStake\": \"35020000000000\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerStakeIncreased>(deserialized);
        Assert.Equal("3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe", typed.Account.AsString);
        Assert.Equal<ulong>(26, typed.BakerId);
        Assert.Equal<ulong>(35020000000000, typed.NewStake.MicroCcdValue);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerStakeDecreased()
    {
        var json = "{\"tag\": \"BakerStakeDecreased\", \"account\": \"3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe\", \"bakerId\": 26, \"newStake\": \"34000000000000\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerStakeDecreased>(deserialized);
        Assert.Equal("3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe", typed.Account.AsString);
        Assert.Equal<ulong>(26, typed.BakerId);
        Assert.Equal<ulong>(34000000000000, typed.NewStake.MicroCcdValue);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerSetRestakeEarnings()
    {
        var json = "{\"tag\": \"BakerSetRestakeEarnings\", \"account\": \"3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe\", \"bakerId\": 26, \"restakeEarnings\": true}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerSetRestakeEarnings>(deserialized);
        Assert.Equal("3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe", typed.Account.AsString);
        Assert.Equal<ulong>(26, typed.BakerId);
        Assert.True(typed.RestakeEarnings);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerKeysUpdated()
    {
        var json = "{\"tag\": \"BakerKeysUpdated\", \"account\": \"3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe\", \"bakerId\": 26, \"signKey\": \"088a92b4d7d6e97904a17e0de90e592db626a58a9f65534128892a2b5da61235\", \"electionKey\": \"85b019ec1bb57a5f0cb50d70669bf3c2f230b9048d73d009df372e754e4431ee\", \"aggregationKey\": \"90c69b4dbc6c0410a38585bca25b75cc60ac83593a78bb385d469dbb74429154ba8c6a54aff95904043a676277239c810b03c2f75777aabc88894738835141618f1c94fa96bfa22bdf47cff6b2ad34399671f912eb6f7724335ee56016215d18\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerKeysUpdated>(deserialized);
        Assert.Equal("3DJoe7aUwMwVmdFdRU2QsnJfsBbCmQu1QHvEg7YtWFZWmsoBXe", typed.Account.AsString);
        Assert.Equal<ulong>(26, typed.BakerId);
        Assert.Equal("088a92b4d7d6e97904a17e0de90e592db626a58a9f65534128892a2b5da61235", typed.SignKey);
        Assert.Equal("85b019ec1bb57a5f0cb50d70669bf3c2f230b9048d73d009df372e754e4431ee", typed.ElectionKey);
        Assert.Equal("90c69b4dbc6c0410a38585bca25b75cc60ac83593a78bb385d469dbb74429154ba8c6a54aff95904043a676277239c810b03c2f75777aabc88894738835141618f1c94fa96bfa22bdf47cff6b2ad34399671f912eb6f7724335ee56016215d18", typed.AggregationKey);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_CredentialKeysUpdated()
    {
        // NOTE: No example of this event existed at time of writing this (mainnet or testnet). So this is a constructed example!
        var json = "{\"tag\": \"CredentialKeysUpdated\", \"credId\": \"b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<CredentialKeysUpdated>(deserialized);
        Assert.Equal("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d", typed.CredId);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_NewEncryptedAmount()
    {
        var json = "{\"tag\": \"NewEncryptedAmount\", \"account\": \"4cDPTiX2FK6ZX5jLo5ggXHGtdwTXxvLGH8ikVX6xdrx35JJRjH\", \"newIndex\": 1, \"encryptedAmount\": \"b964184151f4d0b2a02547628e08612e72fad18dcea1d1f320749d388b8215f52954b7d9f164d44304a0699da8c9f1e6b7b7aaba64731cd25b0952a7cff4636eb9f77d713417e2440c3c0263b79ada0a9a2ec8099bc3344879fad78f7fd08774ad23c246763936090c68c67bfe9c2949842b72c96940843e8372cb0aa590aeef10b5973d309b4e7d850c4a364477b710a44afc626de774095ead86dfd39a851d8810a626d0dfe78309b072de96d001469d81a1540e092332978c044df5b572df\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<NewEncryptedAmount>(deserialized);
        Assert.Equal("4cDPTiX2FK6ZX5jLo5ggXHGtdwTXxvLGH8ikVX6xdrx35JJRjH", typed.Account.AsString);
        Assert.Equal<ulong>(1, typed.NewIndex);
        Assert.Equal("b964184151f4d0b2a02547628e08612e72fad18dcea1d1f320749d388b8215f52954b7d9f164d44304a0699da8c9f1e6b7b7aaba64731cd25b0952a7cff4636eb9f77d713417e2440c3c0263b79ada0a9a2ec8099bc3344879fad78f7fd08774ad23c246763936090c68c67bfe9c2949842b72c96940843e8372cb0aa590aeef10b5973d309b4e7d850c4a364477b710a44afc626de774095ead86dfd39a851d8810a626d0dfe78309b072de96d001469d81a1540e092332978c044df5b572df", typed.EncryptedAmount);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_EncryptedAmountsRemoved()
    {
        var json = "{\"tag\": \"EncryptedAmountsRemoved\", \"account\": \"3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH\", \"newAmount\": \"8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2\", \"upToIndex\": 0, \"inputAmount\": \"acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<EncryptedAmountsRemoved>(deserialized);
        Assert.Equal("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH", typed.Account.AsString);
        Assert.Equal("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2", typed.NewAmount);
        Assert.Equal("acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074", typed.InputAmount);
        Assert.Equal<ulong>(0, typed.UpToIndex);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_AmountAddedByDecryption()
    {
        var json = "{\"tag\": \"AmountAddedByDecryption\", \"amount\": \"200000000\", \"account\": \"3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<AmountAddedByDecryption>(deserialized);
        Assert.Equal(CcdAmount.FromMicroCcd(200000000), typed.Amount);
        Assert.Equal("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH", typed.Account.AsString);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_EncryptedSelfAmountAdded()
    {
        var json = "{\"tag\": \"EncryptedSelfAmountAdded\", \"amount\": \"467000000\", \"account\": \"3BGUHdcD1iSYVacaNkTRBsiJXmmZgmSKdsfzjswMsvwHMusFuU\", \"newAmount\": \"c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000b82e21bde3b1a30dc4cc0d474afc57633430843788fc8ae5fb76a0a8bd7388614bfac03140194978443cadc296453222c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<EncryptedSelfAmountAdded>(deserialized);
        Assert.Equal(CcdAmount.FromMicroCcd(467000000), typed.Amount);
        Assert.Equal("3BGUHdcD1iSYVacaNkTRBsiJXmmZgmSKdsfzjswMsvwHMusFuU", typed.Account.AsString);
        Assert.Equal("c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000b82e21bde3b1a30dc4cc0d474afc57633430843788fc8ae5fb76a0a8bd7388614bfac03140194978443cadc296453222c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", typed.NewAmount);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_UpdateEnqueued()
    {
        var json = "{\"tag\": \"UpdateEnqueued\", \"payload\": {\"updateType\": \"level1\", \"update\": {\"typeOfUpdate\": \"level2KeysUpdate\", \"updatePayload\": {\"keys\": [{\"schemeId\": \"Ed25519\", \"verifyKey\": \"0fb2431e05980f143dd5b6e7e197aa3b8b4ab666b66be64c7f641dc5343e80a6\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"3b9022f1625f06795255489bfeb6ee6244a16991f4fa5cef9c4f4b6614eeb5cf\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"fff862a666372843e3d05514573a9ecf87e9258bda7a2df908962eec53611dfc\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"af65154d71176544869a01eee6195a3cd15a2e135bbf208b5f5f50867674fe07\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"be3322e2b3e7ff4f4ab1e9251bfc3e75024e2546b2aec36b5e754a7fc1b7629e\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"6d4924a5da84615352dd6e5f19bf58157838dbd4f2b9e67713fb3f6e39d32a44\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"8cba5dcc0ef47b69118dfacc695ee36faf845bb1963e80448297ce0087305d6d\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"563133c8df10a3a2d88522eb62629b9dac3dcafbe41a6f9419287755f93524ed\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"eb89caed1020683d47e33c4457aa2285f1ef8cda92f4cbba861aabf9d6508cda\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"3efd31536ee2b0453ea0553817f80ff1f94ae3d329a8d368dab998d04ba56e31\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"31f39c8851718bd104ce1d166d73305668cd2618ccf5f77f8b5206dc36005e90\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"cf78e2c726d31d3fe0ed3c32c44174de53a63885a0fd0f583a3d4777ea6ac39f\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"ff20982a805c847e6418f1b7cf199e20f1f7c6c7e0453f8342977671b323e134\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"802154292370cf24b1b408f1002d2ab3d7efea7fdec9bc8cbb1c6472421c9a49\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"8c094013f41d80b3c1d301a1c206b26a8865438985341946be6c0f35d5567743\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"e0e706096a1371af1f026c70069c5bc546d7e51c1a6b009818c874599ba868c9\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"b7118672d789106e1529d4c8f37da5b79acbdd8ed5dfe63fc8649588c3115459\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"e41da9c37cfe9867061cb3551573bbb2b0bba92a56a8c9ec91d0e9f4e8d87ae3\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"e01fe85030814112973b42acea20bafc2e88a3c141241192b938ea5a8f253c29\"}, {\"schemeId\": \"Ed25519\", \"verifyKey\": \"e0e706096a1371af1f026c70069c5bc546d7e51c1a6b009818c874599ba868c9\"}], \"protocol\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"emergency\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"euroPerEnergy\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"microGTUPerEuro\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"paramGASRewards\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"mintDistribution\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"foundationAccount\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"electionDifficulty\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"addAnonymityRevoker\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"addIdentityProvider\": {\"threshold\": 7, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]}, \"bakerStakeThreshold\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}, \"transactionFeeDistribution\": {\"threshold\": 2, \"authorizedKeys\": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]}}}}, \"effectiveTime\": 1624630671}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<UpdateEnqueued>(deserialized);
        typed.EffectiveTime.Should().Be(new UnixTimeSeconds(1624630671));
        var nested = Assert.IsType<Level1UpdatePayload>(typed.Payload);
        Assert.IsType<Level2KeysLevel1Update>(nested.Content);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_TransferredWithSchedule()
    {
        var json = "{\"to\": \"4TFVnybZqYj1HWn6UnGMomre1EYwTQDFk5ha5fYQbPypcjrozp\", \"tag\": \"TransferredWithSchedule\", \"from\": \"3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH\", \"amount\": [[1621260359000, \"1000000\"], [1621611359000, \"2000000\"], [1639438559000, \"3000000\"]]}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<TransferredWithSchedule>(deserialized);
        Assert.Equal("4TFVnybZqYj1HWn6UnGMomre1EYwTQDFk5ha5fYQbPypcjrozp", typed.To.AsString);
        Assert.Equal("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH", typed.From.AsString);
        Assert.Equal(3, typed.Amount.Length);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1621260359000), typed.Amount[0].Timestamp);
        Assert.Equal(CcdAmount.FromMicroCcd(1000000), typed.Amount[0].Amount);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1621611359000), typed.Amount[1].Timestamp);
        Assert.Equal(CcdAmount.FromMicroCcd(2000000), typed.Amount[1].Amount);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_CredentialsUpdated()
    {
        var json = "{\"tag\": \"CredentialsUpdated\", \"account\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\", \"newCredIds\": [\"87b8abcd2df0481aa04e7b6c436b05b3375f2af03da94e13d73bfeac451c51a8fe3865dc6c59dca21c71a5349a6dbc7e\"], \"newThreshold\": 1, \"removedCredIds\": []}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<CredentialsUpdated>(deserialized);
        Assert.Equal("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN", typed.Account.AsString);
        Assert.Equal(new [] {"87b8abcd2df0481aa04e7b6c436b05b3375f2af03da94e13d73bfeac451c51a8fe3865dc6c59dca21c71a5349a6dbc7e"}, typed.NewCredIds);
        Assert.Equal(1 , typed.NewThreshold);
        Assert.Empty(typed.RemovedCredIds);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_DataRegistered()
    {
        var json = "{\"tag\": \"DataRegistered\", \"data\": \"784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DataRegistered>(deserialized);
        Assert.Equal(RegisteredData.FromHexString("784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863"), typed.Data);
    
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_TransferMemo()
    {
        var json = "{\"tag\": \"TransferMemo\", \"memo\": \"704164616d2042696c6c696f6e61697265\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<TransferMemo>(deserialized);
        Assert.Equal(Memo.CreateFromHex("704164616d2042696c6c696f6e61697265"), typed.Memo);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_Interrupted()
    {
        var json = @"{
                        ""tag"": ""Interrupted"",
                        ""address"": {
                            ""subindex"": 2,
                            ""index"": 5040
                        },
                        ""events"": [
                            ""fd00070000000000000000d116251dba02ec447b5fee61b48d920a32dc96a645686a8d8beed3d71ed5843d""
                        ]
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<Interrupted>(deserialized);
        typed.Address.Should().Be(new ContractAddress(5040, 2));
        typed.Events.Should().ContainSingle().Which.AsHexString.Should().Be("fd00070000000000000000d116251dba02ec447b5fee61b48d920a32dc96a645686a8d8beed3d71ed5843d");

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_Resumed()
    {
        var json = @"{
                        ""tag"": ""Resumed"",
                        ""success"": true,
                        ""address"": {
                            ""subindex"": 3,
                            ""index"": 5040
                        }
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<Resumed>(deserialized);
        typed.Address.Should().Be(new ContractAddress(5040, 3));
        typed.Success.Should().BeTrue();
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_Upgraded()
    {
        var json = @"{
						""address"": { ""index"": 1039, ""subindex"": 0 },
						""from"": ""73ba390d9ce2bb1bf54f124bb00e9dee0d6dc40d6de0f5ba06e1d1f095e4afcc"",
						""tag"": ""Upgraded"",
						""to"": ""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""
					}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<Upgraded>(deserialized);
        typed.Address.Should().Be(new ContractAddress(1039, 0));
        typed.From.Should().Be(new ModuleRef("73ba390d9ce2bb1bf54f124bb00e9dee0d6dc40d6de0f5ba06e1d1f095e4afcc"));
        typed.To.Should().Be(new ModuleRef("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerSetOpenStatus()
    {
        var json = "{\"bakerId\": 27,\"tag\": \"BakerSetOpenStatus\",\"account\": \"44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH\",\"openStatus\": \"openForAll\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerSetOpenStatus>(deserialized);
        typed.BakerId.Should().Be(27);
        typed.Account.AsString.Should().Be("44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH");
        typed.OpenStatus.Should().Be(BakerPoolOpenStatus.OpenForAll);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerSetMetadataURL()
    {
        var json = @"{
                        ""bakerId"": 27,
                        ""tag"": ""BakerSetMetadataURL"",
                        ""account"": ""44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH"",
                        ""metadataURL"": ""Very good url""
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerSetMetadataURL>(deserialized);
        typed.BakerId.Should().Be(27);
        typed.Account.AsString.Should().Be("44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH");
        typed.MetadataURL.Should().Be("Very good url");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerSetTransactionFeeCommission()
    {
        var json = "{\"bakerId\": 27,\"tag\": \"BakerSetTransactionFeeCommission\",\"account\": \"44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH\",\"transactionFeeCommission\":0.05}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerSetTransactionFeeCommission>(deserialized);
        typed.BakerId.Should().Be(27);
        typed.Account.AsString.Should().Be("44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH");
        typed.TransactionFeeCommission.Should().Be(0.05m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }

    [Fact]
    public void RoundTrip_BakerSetBakingRewardCommission()
    {
        var json = "{\"bakerId\": 27,\"bakingRewardCommission\": 0.05,\"tag\": \"BakerSetBakingRewardCommission\",\"account\": \"44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerSetBakingRewardCommission>(deserialized);
        typed.BakerId.Should().Be(27);
        typed.Account.AsString.Should().Be("44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH");
        typed.BakingRewardCommission.Should().Be(0.05m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_BakerSetFinalizationRewardCommission()
    {
        var json = "{\"bakerId\": 27,\"tag\": \"BakerSetFinalizationRewardCommission\",\"finalizationRewardCommission\": 1.0,\"account\": \"44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<BakerSetFinalizationRewardCommission>(deserialized);
        typed.BakerId.Should().Be(27);
        typed.Account.AsString.Should().Be("44Ernz8GQrPvPSDRiC59xQE2GsXPDok9hLKU9KTVteH4xq9HyH");
        typed.FinalizationRewardCommission.Should().Be(1.0m);
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DelegationAdded()
    {
        var json = "{\"tag\": \"DelegationAdded\",\"account\": \"4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS\",\"delegatorId\": 28}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DelegationAdded>(deserialized);
        typed.DelegatorId.Should().Be(28);
        typed.Account.AsString.Should().Be("4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DelegationSetRestakeEarnings()
    {
        var json = "{\"restakeEarnings\": true,\"tag\": \"DelegationSetRestakeEarnings\",\"account\": \"4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS\",\"delegatorId\": 28}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DelegationSetRestakeEarnings>(deserialized);
        typed.DelegatorId.Should().Be(28);
        typed.Account.AsString.Should().Be("4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS");
        typed.RestakeEarnings.Should().BeTrue();
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DelegationSetDelegationTarget()
    {
        var json = @"{
                        ""tag"": ""DelegationSetDelegationTarget"",
                        ""delegationTarget"": {
                            ""bakerId"": 27,
                            ""delegateType"": ""Baker""
                        },
                        ""account"": ""4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS"",
                        ""delegatorId"": 28
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DelegationSetDelegationTarget>(deserialized);
        typed.DelegatorId.Should().Be(28);
        typed.Account.AsString.Should().Be("4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS");
        typed.DelegationTarget.Should().Be(new BakerDelegationTarget(27));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DelegationRemoved()
    {
        var json = @"{
                        ""tag"": ""DelegationRemoved"",
                        ""account"": ""4nbn4c461GRJfJncG96FpUxbbxxUV1R8yu8XgzZRkt876PDD6m"",
                        ""delegatorId"": 29
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DelegationRemoved>(deserialized);
        typed.DelegatorId.Should().Be(29);
        typed.Account.AsString.Should().Be("4nbn4c461GRJfJncG96FpUxbbxxUV1R8yu8XgzZRkt876PDD6m");
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DelegationStakeIncreased()
    {
        var json = @"{
                        ""tag"": ""DelegationStakeIncreased"",
                        ""account"": ""4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS"",
                        ""newStake"": ""100000000"",
                        ""delegatorId"": 27
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DelegationStakeIncreased>(deserialized);
        typed.DelegatorId.Should().Be(27);
        typed.Account.AsString.Should().Be("4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS");
        typed.NewStake.Should().Be(CcdAmount.FromMicroCcd(100000000));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_DelegationStakeDecreased()
    {
        var json = @"{
                        ""tag"": ""DelegationStakeDecreased"",
                        ""account"": ""4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS"",
                        ""newStake"": ""90000000"",
                        ""delegatorId"": 28
                    }";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<DelegationStakeDecreased>(deserialized);
        typed.DelegatorId.Should().Be(28);
        typed.Account.AsString.Should().Be("4hbWAFJTSwYdt4ArhzAmCLdUYfnrf9C7EPNbc2Dt4bS4rUxhiS");
        typed.NewStake.Should().Be(CcdAmount.FromMicroCcd(90000000));
        
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}