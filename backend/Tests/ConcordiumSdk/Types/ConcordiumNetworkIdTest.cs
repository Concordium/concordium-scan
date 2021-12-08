using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class ConcordiumNetworkIdTest
{
    [Fact] 
    public void Mainnet()
    {
        var target = ConcordiumNetworkId.Mainnet;
        
        Assert.Equal(new BlockHash("9dd9ca4d19e9393877d2c44b70f89acbfc0883c2243e5eeaecc0d1cd0503f478"), target.GenesisBlockHash);
        Assert.Equal("Mainnet", target.NetworkName);
    }
    
    [Fact]
    public void Testnet()
    {
        var target = ConcordiumNetworkId.Testnet;
        
        Assert.Equal(new BlockHash("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d"), target.GenesisBlockHash);
        Assert.Equal("Testnet", target.NetworkName);
    }
    
    [Fact]
    public void GetFromGenesisBlockHash_TestnetGenesisBlockHash()
    {
        var testnetGenesisBlock = new BlockHash("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d");
        var result = ConcordiumNetworkId.GetFromGenesisBlockHash(testnetGenesisBlock);
        Assert.Same(ConcordiumNetworkId.Testnet, result);
    }
    
    [Fact]
    public void GetFromGenesisBlockHash_MainnetGenesisBlockHash()
    {
        var mainnetGenesisBlock = new BlockHash("9dd9ca4d19e9393877d2c44b70f89acbfc0883c2243e5eeaecc0d1cd0503f478");
        var result = ConcordiumNetworkId.GetFromGenesisBlockHash(mainnetGenesisBlock);
        Assert.Same(ConcordiumNetworkId.Mainnet, result);
    }

    [Fact]
    public void GetFromGenesisBlockHash_UnknownBlockHash()
    {
        var randomBlockHash = new BlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1");
        Assert.Throws<InvalidOperationException>(() => ConcordiumNetworkId.GetFromGenesisBlockHash(randomBlockHash));
    }

    [Theory]
    [InlineData("Mainnet")]
    [InlineData("mainnet")]
    [InlineData("MAINNET")]
    public void GetFromNetworkName_Mainnet(string input)
    {
        var result = ConcordiumNetworkId.GetFromNetworkName(input);
        Assert.Same(ConcordiumNetworkId.Mainnet, result);
    }
    
    [Theory]
    [InlineData("Testnet")]
    [InlineData("testnet")]
    [InlineData("TESTNET")]
    public void GetFromNetworkName_Testnet(string input)
    {
        var result = ConcordiumNetworkId.GetFromNetworkName(input);
        Assert.Same(ConcordiumNetworkId.Testnet, result);
    }

    [Fact]
    public void GetFromNetworkName_UnknownNetworkName()
    {
        Assert.Throws<InvalidOperationException>(() => ConcordiumNetworkId.GetFromNetworkName("foo-bar"));
    }
}