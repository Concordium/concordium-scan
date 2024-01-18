using NBitcoin.DataEncoders;

namespace Application.Utils;

internal static class Base58Encoder
{
    internal static Base58CheckEncoder Base58CheckEncoder = new Base58CheckEncoder();

}
