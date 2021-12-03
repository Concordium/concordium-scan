using ConcordiumSdk.ExportedMobileWalletFile;

namespace Tests.ConcordiumSdk.ExportedMobileWalletFile;

public class MobileWalletExportReaderTest
{
    [Fact(Skip = "Intended for experimenting with decrypting specific wallet file manually")]
    public void ReadAndDecrypt()
    {
        var walletFilePath = @"<path to an exported mobile wallet file>";
        var password = "<password for the exported mobile wallet file>";
        var target = MobileWalletExportReader.ReadAndDecrypt(walletFilePath, password);
        
        Assert.NotNull(target);
    }
    
}