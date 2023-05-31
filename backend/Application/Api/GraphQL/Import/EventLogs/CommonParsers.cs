using System.IO;
using System.Numerics;
using Concordium.Sdk.Types;
using NBitcoin.DataEncoders;

namespace Application.Api.GraphQL.Import.EventLogs
{
    public class CommonParsers
    {
        /// <summary>
        /// Parses Token Id from input bytes
        /// <see href="https://proposals.concordium.software/CIS/cis-2.html#tokenid"/>
        /// </summary>
        /// <param name="st"><see cref="BinaryReader"/></param>
        /// <returns>HEX serialzed Token Id</returns>
        public static string ParseTokenId(BinaryReader st)
        {
            var tokenIdSize = (int)st.ReadByte();
            return Convert.ToHexString(st.ReadBytes(tokenIdSize));
        }

        /// <summary>
        /// Parses Token Amount from input bytes
        /// <see href="https://proposals.concordium.software/CIS/cis-2.html#tokenamount"/>
        /// </summary>
        /// <param name="st"><see cref="BinaryReader"/></param>
        /// <returns>Parsed Amount</returns>
        public static BigInteger ParseTokenAmount(BinaryReader st)
        {
            BigInteger value = new BigInteger(0);
            int shift = 0;

            while (true)
            {
                var bt = st.ReadByte();
                value += new BigInteger(bt & 0x7f) << shift;

                if (bt < 128)
                    break;

                shift += 7;
            }

            return value;
        }

        /// <summary>
        /// Parses Address from input bytes
        /// <see href="https://proposals.concordium.software/CIS/cis-2.html#address"/>
        /// </summary>
        /// <param name="st"><see cref="BinaryReader"/></param>
        /// <returns>Parsed Address</returns>
        public static BaseAddress ParseAddress(BinaryReader st)
        {
            byte type = st.ReadByte();
            switch (type)
            {
                case (int)CisEventAddressType.AccountAddress:
                    return new CisEventAddressAccount()
                    {
                        Address = AccountAddress.From(st.ReadBytes(32))
                    };
                case (int)CisEventAddressType.ContractAddress:
                    return new CisEventAddressContract()
                    {
                        Index = st.ReadUInt64(),
                        SubIndex = st.ReadUInt64()
                    };
                default:
                    throw new Exception(String.Format("Invalid Address Type : {0}", type));
            }
        }

        /// <summary>
        /// Parses Metadata URL input bytes
        /// <see href="https://proposals.concordium.software/CIS/cis-2.html#metadataurl"/>
        /// </summary>
        /// <param name="st"><see cref="BinaryReader"/></param>
        /// <returns>Parsed Metadata URL</returns>
        public static string ParseMetadataUrl(BinaryReader st)
        {
            var size = st.ReadInt16();
            var urlBytes = st.ReadBytes(size);
            return Encoders.ASCII.EncodeData(urlBytes);
        }
    }
}
