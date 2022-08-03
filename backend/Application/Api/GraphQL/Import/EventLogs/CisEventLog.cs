using System.IO;
using System.Numerics;
using NBitcoin.DataEncoders;

namespace Application.Api.GraphQL.Import.EventLogs
{
    public abstract class CisEvent
    {
        private const int MAX_7_BIT_VALUE = 128;

        public CisEvent(CisEventType type)
        {
            this.Type = type;
        }

        public ulong ContractIndex { get; init; }
        public ulong ContractSubIndex { get; init; }
        public CisEventType Type { get; private set; }
        public string TokenId { get; init; }

        public static bool TryParse(byte[] eventBytes, ConcordiumSdk.Types.ContractAddress address, out CisEvent cisEvent)
        {
            if (!IsCisEvent(eventBytes))
            {
                cisEvent = null;
                return false;
            }

            try
            {
                cisEvent = CisEvent.Parse(address, eventBytes);
                return true;
            }
            catch (System.Exception ex)
            {
                cisEvent = null;
                return false;
            }
        }

        public static bool IsCisEvent(byte[] eventBytes)
        {
            //todo: convert to constant
            var allowedEventTypes = new List<int> {
                (int)CisEventType.Burn,
                (int)CisEventType.Mint,
                (int)CisEventType.TokenMetadata,
                (int)CisEventType.Transfer,
                (int)CisEventType.UpdateOperator
            };

            return allowedEventTypes.Contains((int)eventBytes.FirstOrDefault());
        }

        private static CisEvent Parse(ConcordiumSdk.Types.ContractAddress address, byte[] eventBytes)
        {
            var st = new BinaryReader(new MemoryStream(eventBytes));
            var eventType = st.ReadByte();
            switch (eventType)
            {
                case ((int)CisEventType.Burn):
                    return ParseBurnEvent(address, st);
                case ((int)CisEventType.Mint):
                    return ParseMintEvent(address, st);
                case ((int)CisEventType.TokenMetadata):
                    return ParseTokenMetadatatEvent(address, st);
                case ((int)CisEventType.Transfer):
                    return ParseTransferEvent(address, st);
                case ((int)CisEventType.UpdateOperator):
                    return ParseUpdateOperatorEvent(address, st);
                default:
                    throw new Exception(String.Format("invalid event type: {0}", eventType));
            }
        }

        private static CisEvent ParseUpdateOperatorEvent(ConcordiumSdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisUpdateOperatorEvent()
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                Update = ParseOperatorUpdate(st),
                Owner = ParseAddress(st),
                Operator = ParseAddress(st)
            };
        }

        private static CisTransferEvent ParseTransferEvent(ConcordiumSdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisTransferEvent()
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                TokenId = ParseTokenId(st),
                TokenAmount = ParseTokenAmount(st),
                FromAddress = ParseAddress(st),
                ToAddress = ParseAddress(st)
            };
        }

        private static CisEvent ParseTokenMetadatatEvent(ConcordiumSdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisTokenMetadataEvent()
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                TokenId = ParseTokenId(st),
                MetadataUrl = ParseMetadataUrl(st),
                HashHex = (st.ReadByte() == 1) ? Convert.ToHexString(st.ReadBytes(32)) : null
            };
        }

        private static CisMintEvent ParseMintEvent(ConcordiumSdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisMintEvent
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                TokenId = ParseTokenId(st),
                TokenAmount = ParseTokenAmount(st),
                ToAddress = ParseAddress(st),
            };
        }

        private static CisBurnEvent ParseBurnEvent(ConcordiumSdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisBurnEvent
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                // https://proposals.concordium.software/CIS/cis-1.html#tokenid
                TokenId = ParseTokenId(st),
                TokenAmount = ParseTokenAmount(st),
                FromAddress = ParseAddress(st),
            };
        }

        private static string ParseTokenId(BinaryReader st)
        {
            var tokenIdSize = (int)st.ReadByte();
            return Convert.ToHexString(st.ReadBytes(tokenIdSize));
        }

        public static BigInteger ParseTokenAmount(BinaryReader st)
        {
            BigInteger value = new BigInteger(0);
            int shift = 0;

            while (true)
            {
                var bt = st.ReadByte();
                value += (bt & 0x7f) << shift;

                if (bt < 128)
                    break;

                shift += 7;
            }

            return value;
        }

        private static BaseAddress ParseAddress(BinaryReader st)
        {
            byte type = st.ReadByte();
            switch (type)
            {
                case (int)CisEventAddressType.AccountAddress:
                    return new CisEventAddressAccount()
                    {
                        Address = new ConcordiumSdk.Types.AccountAddress(st.ReadBytes(32))
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

        private static string ParseMetadataUrl(BinaryReader st)
        {
            var size = st.ReadInt16();
            var urlBytes = st.ReadBytes(size);
            return Encoders.ASCII.EncodeData(urlBytes);
        }

        private static OperatorUpdateType ParseOperatorUpdate(BinaryReader st)
        {
            var type = st.ReadByte();
            switch (type)
            {
                case 0: return OperatorUpdateType.RemoveOperator;
                case 1: return OperatorUpdateType.AddOperator;
                default: throw new Exception(String.Format("Invalid Operator update type: {0}", type));
            }
        }
    }

    internal class CisUpdateOperatorEvent : CisEvent
    {
        public CisUpdateOperatorEvent() : base(CisEventType.UpdateOperator)
        {
        }

        public OperatorUpdateType Update { get; set; }
        public BaseAddress Owner { get; set; }
        public BaseAddress Operator { get; set; }
    }

    public class CisTokenMetadataEvent : CisEvent
    {
        public CisTokenMetadataEvent() : base(CisEventType.TokenMetadata)
        {
        }

        public string MetadataUrl { get; set; }
        public string? HashHex { get; set; }
    }

    public enum CisEventAddressType
    {
        AccountAddress = 0,
        ContractAddress = 1,
    }

    public enum CisEventType
    {
        TokenMetadata = 251,
        UpdateOperator = 252,
        Burn = 253,
        Mint = 254,
        Transfer = 255,
    }

    public class CisBurnEvent : CisEvent
    {
        public CisBurnEvent() : base(CisEventType.Burn)
        {
        }

        public BigInteger TokenAmount { get; init; }
        public BaseAddress FromAddress { get; init; }
    }

    public class CisMintEvent : CisEvent
    {
        public CisMintEvent() : base(CisEventType.Mint)
        {
        }

        public BigInteger TokenAmount { get; internal set; }
        public BaseAddress ToAddress { get; internal set; }
    }

    public class CisTransferEvent : CisEvent
    {
        public CisTransferEvent() : base(CisEventType.Transfer)
        {
        }

        public BigInteger TokenAmount { get; set; }
        public BaseAddress FromAddress { get; set; }
        public BaseAddress ToAddress { get; set; }
    }

    public abstract class BaseAddress
    {

        public BaseAddress(CisEventAddressType type)
        {
            this.Type = type;
        }

        public CisEventAddressType Type { get; private set; }
    }

    public class CisEventAddressContract : BaseAddress
    {
        public CisEventAddressContract() : base(CisEventAddressType.ContractAddress)
        {
        }

        public ulong Index { get; set; }
        public ulong SubIndex { get; set; }
    }

    public class CisEventAddressAccount : BaseAddress
    {
        public CisEventAddressAccount() : base(CisEventAddressType.AccountAddress)
        {
        }

        public ConcordiumSdk.Types.AccountAddress Address { get; set; }
    }

    public enum OperatorUpdateType
    {
        RemoveOperator = 0,
        AddOperator = 1
    }
}