namespace ConcordiumSdk.Types;

public class ConcordiumNetworkId
{
    public string NetworkName { get; }
    public BlockHash GenesisBlockHash { get; }

    private ConcordiumNetworkId(string networkName, BlockHash genesisBlockHash)
    {
        NetworkName = networkName;
        GenesisBlockHash = genesisBlockHash;
    }

    public static ConcordiumNetworkId Mainnet { get; } = new("Mainnet", new BlockHash("9dd9ca4d19e9393877d2c44b70f89acbfc0883c2243e5eeaecc0d1cd0503f478")); 
    public static ConcordiumNetworkId Testnet { get; } = new("Testnet", new BlockHash("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d"));

    public static ConcordiumNetworkId GetFromGenesisBlockHash(BlockHash genesisBlockHash)
    {
        var result = TryGetFromGenesisBlockHash(genesisBlockHash);
        return result ?? throw new InvalidOperationException("Given block hash is not a known genesis block hash.");
    }
    
    public static ConcordiumNetworkId? TryGetFromGenesisBlockHash(BlockHash genesisBlockHash)
    {
        if (genesisBlockHash == null) throw new ArgumentNullException(nameof(genesisBlockHash));
        if (genesisBlockHash == Mainnet.GenesisBlockHash)
            return Mainnet;
        if (genesisBlockHash == Testnet.GenesisBlockHash)
            return Testnet;
        return null;
    }
    
    public override string ToString()
    {
        return NetworkName;
    }

    public static ConcordiumNetworkId GetFromNetworkName(string networkName)
    {
        if (networkName == null) throw new ArgumentNullException(nameof(networkName));
        if (networkName.Equals(Mainnet.NetworkName, StringComparison.InvariantCultureIgnoreCase))
            return Mainnet;
        if (networkName.Equals(Testnet.NetworkName, StringComparison.InvariantCultureIgnoreCase))
            return Testnet;
        throw new InvalidOperationException("Given network name is not a known network name");
    }
}