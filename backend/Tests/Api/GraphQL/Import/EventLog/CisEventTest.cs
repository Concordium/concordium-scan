using System.Numerics;
using Application.Api.GraphQL.Import.EventLogs;
using Concordium.Sdk.Types;
using FluentAssertions;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

namespace Tests.Api.GraphQL.Import.EventLog
{
    public class CisEventTest
    {
        [Fact]
        public void ShouldRecognizeMintEvent()
        {
            var isCisEvent = CisEvent.IsCisEvent(new byte[] { (int)CisEventType.Mint });
            isCisEvent.Should().BeTrue("Should Recognize Mint Event");
        }

        [Fact]
        public void ShouldParseMintEvent()
        {
            var contractAddess = ContractAddress.From(1, 0);
            CisEvent cisEvent;
            byte[] eventBytes = Convert.FromHexString("fe040000000101009d230671ab6efaf2861f0b5942e650186036b8fbb4e9973f5634b43e664d3b4b");
            CisEvent.TryParse(eventBytes, contractAddess, 1L, out cisEvent);

            cisEvent.Should().NotBeNull();
            
            cisEvent.Should().BeOfType<CisMintEvent>();
            var mintEvent = cisEvent as CisMintEvent;
            mintEvent!.TokenId.Should().Be("00000001");
            mintEvent.ToAddress.Should().BeOfType<AccountAddress>();
            mintEvent.ToAddress.As<AccountAddress>().AsString
                .Should().Be("48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e");
            mintEvent.TokenAmount.Should().Be(new BigInteger(1));
        }

        [Fact]
        public void ShouldParseTransferEvent()
        {
            var contractAddess = ContractAddress.From(1, 0);
            CisEvent cisEvent;
            byte[] eventBytes = Convert.FromHexString(
                "ff040000000101009d230671ab6efaf2861f0b5942e650186036b8fbb4e9973f5634b43e664d3b4b009a24cbfa7d436c36def76154006e20c30c1a8213d02ee7971f5f65cf1e4206e7"
            );
            CisEvent.TryParse(eventBytes, contractAddess, 1L, out cisEvent);

            cisEvent.Should().NotBeNull();
            cisEvent.Should().BeOfType<CisTransferEvent>();

            var transferEvent = cisEvent as CisTransferEvent;
            transferEvent!.TokenId.Should().Be("00000001");
            transferEvent.FromAddress.Should().BeOfType<AccountAddress>();
            transferEvent.FromAddress.As<AccountAddress>().AsString
                .Should().Be("48x2Uo8xCMMxwGuSQnwbqjzKtVqK5MaUud4vG7QEUgDmYkV85e");
            transferEvent.TokenAmount.Should().Be(new BigInteger(1));
            transferEvent.ToAddress.Should().BeOfType<AccountAddress>();
            transferEvent.ToAddress.As<AccountAddress>().AsString
                .Should().Be("47da8rxVf4vFuF21hFypBJ3eGibxGSuricuAHnUpVbZjLeB4ML");
        }

        [Fact]
        public void ShouldParseTokenMetadataEvent()
        {
            var contractAddess = ContractAddress.From(1, 0);
            CisEvent cisEvent;
            byte[] eventBytes = Convert.FromHexString(
                "fb0400000001540068747470733a2f2f697066732e696f2f697066732f516d563552454533484a524c5448646d71473138576335504246334e6339573564514c345270374d7842737838713f66696c656e616d653d6e66742e6a706700"
            );
            CisEvent.TryParse(eventBytes, contractAddess, 1L, out cisEvent);

            cisEvent.Should().NotBeNull();
            cisEvent.Should().BeOfType<CisTokenMetadataEvent>();

            var metadataEvent = cisEvent as CisTokenMetadataEvent;
            metadataEvent!.TokenId.Should().Be("00000001");
            metadataEvent.HashHex.Should().BeNull();
            metadataEvent.MetadataUrl.Should().Be("https://ipfs.io/ipfs/QmV5REE3HJRLTHdmqG18Wc5PBF3Nc9W5dQL4Rp7MxBsx8q?filename=nft.jpg");
        }


        [Fact]
        public void ShouldParseEvent()
        {
            var contractAddess = ContractAddress.From(1, 0);
            CisEvent cisEvent;
            byte[] eventBytes = Convert.FromHexString(
                    "fd0080a094a58d1d00f761affb26ea6bbd14e4c50e51984d6d059156fa86658126c5ca0b747d60ba00"
            );

            CisEvent.TryParse(eventBytes, contractAddess, 1L, out cisEvent);

            cisEvent.Should().NotBeNull();
            cisEvent.Should().BeOfType<CisBurnEvent>();
            cisEvent.As<CisBurnEvent>().TokenAmount.Should().Be(new BigInteger(1000000000000L));
        }
    }
}
