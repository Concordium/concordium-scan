using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import.EventLogs
{

    public class EventLogHandler
    {
        public EventLogWriter writer;

        public EventLogHandler(EventLogWriter logWriter)
        {
            this.writer = logWriter;
        }

        private readonly record struct PossibleCis2Event(long TxId, Concordium.Sdk.Types.ContractAddress Address, ContractEvent ContractEvent);

        /// <summary>
        /// Fetches log bytes from Transaction, parses them and persists them to the database.
        /// </summary>
        /// <param name="transactions">Pair of Database Persisted Transaction and on chain transaction</param>
        public List<CisAccountUpdate> HandleLogs(TransactionPair[] transactions)
        {
            var events = new List<PossibleCis2Event>();
            foreach (var (blockItemSummary, transaction) in transactions)
            {
                if (blockItemSummary.TryGetContractInit(out var contractInitializedEvent))
                {
                    events.AddRange(
                        contractInitializedEvent!.Events.Select(e =>
                            new PossibleCis2Event(transaction.Id, contractInitializedEvent.ContractAddress, e))
                    );
                }

                if (blockItemSummary.TryGetContractUpdateLogs(out var items))
                {
                    events.AddRange(
                        items!.SelectMany(item => 
                            item.Item2.Select(e => 
                                new PossibleCis2Event(transaction.Id, item.Item1, e)))
                        );
                }
            }

            //Select Cis Events
            var cisEvents = events
                .Select(e => ParseCisEvent(e.TxId, e.Address, e.ContractEvent.Bytes))
                .Where(e => e != null)
                .Cast<CisEvent>();

            //Handle Smart Contract Cis Events Logs
            var updates = cisEvents.Select(e => new
            {
                TokenUpdate = GetTokenUpdates(e),
                AccountUpdates = GetAccountUpdates(e)
            });

            IEnumerable<CisEventTokenUpdate> tokenUpdates = updates
                .Where(u => u.TokenUpdate != null)
                .Select(u => u.TokenUpdate)
                .Cast<CisEventTokenUpdate>();
            var accountUpdates = updates.SelectMany(a => a.AccountUpdates).ToList();

            if (tokenUpdates.Count() > 0)
            {
                writer.ApplyTokenUpdates(tokenUpdates);
            }

            if (accountUpdates.Count() > 0)
            {
                writer.ApplyAccountUpdates(accountUpdates);
            }

            return accountUpdates;
        }

        /// <summary>
        /// Computes Token amount changes for an Account.
        /// </summary>
        /// <param name="log"></param>
        /// <returns>Parsed <see cref="CisAccountUpdate"/></returns>
        private CisAccountUpdate[] GetAccountUpdates(CisEvent log)
        {
            switch (log)
            {
                case CisBurnEvent e:
                    if (e.FromAddress is CisEventAddressAccount accntAddress)
                    {
                        return new CisAccountUpdate[] {
                            new CisAccountUpdate()
                            {
                                ContractIndex = e.ContractIndex,
                                ContractSubIndex = e.ContractSubIndex,
                                TokenId = e.TokenId,
                                AmountDelta = e.TokenAmount * -1,
                                Address = accntAddress.Address
                            }
                        };
                    }

                    return new CisAccountUpdate[] { };
                case CisTransferEvent e:
                    var ret = new List<CisAccountUpdate>();
                    if (e.FromAddress is CisEventAddressAccount accntAddress2)
                    {
                        ret.Add(new CisAccountUpdate()
                        {
                            ContractIndex = e.ContractIndex,
                            ContractSubIndex = e.ContractSubIndex,
                            TokenId = e.TokenId,
                            AmountDelta = e.TokenAmount * -1,
                            Address = accntAddress2.Address
                        });
                    }

                    if (e.ToAddress is CisEventAddressAccount accntAddress3)
                    {
                        ret.Add(new CisAccountUpdate()
                        {
                            ContractIndex = e.ContractIndex,
                            ContractSubIndex = e.ContractSubIndex,
                            TokenId = e.TokenId,
                            AmountDelta = e.TokenAmount,
                            Address = accntAddress3.Address
                        });
                    }

                    return ret.ToArray();
                case CisMintEvent e:
                    if (e.ToAddress is CisEventAddressAccount accntAddress4)
                    {
                        return new CisAccountUpdate[] {new CisAccountUpdate()
                        {
                            ContractIndex = e.ContractIndex,
                            ContractSubIndex = e.ContractSubIndex,
                            TokenId = e.TokenId,
                            AmountDelta = e.TokenAmount,
                            Address = accntAddress4.Address
                        }};
                    }

                    return new CisAccountUpdate[] { };
                default:
                    return new CisAccountUpdate[] { };
            }
        }

        /// <summary>
        /// Computes aggregate Token amount updates for any account.
        /// </summary>
        /// <param name="cisLog">CisEvent</param>
        /// <returns>Parsed <see cref="CisEventTokenUpdate"/></returns>
        private CisEventTokenUpdate? GetTokenUpdates(CisEvent cisLog)
        {
            switch (cisLog)
            {
                case CisBurnEvent log:
                    return new CisEventTokenAmountUpdate()
                    {
                        ContractIndex = log.ContractIndex,
                        ContractSubIndex = log.ContractSubIndex,
                        TokenId = log.TokenId,
                        AmountDelta = log.TokenAmount * -1
                    };
                case CisMintEvent log:
                    return new CisEventTokenAddedUpdate()
                    {
                        ContractIndex = log.ContractIndex,
                        ContractSubIndex = log.ContractSubIndex,
                        TokenId = log.TokenId,
                        AmountDelta = log.TokenAmount
                    };
                case CisTokenMetadataEvent log:
                    return new CisEventTokenMetadataUpdate()
                    {
                        ContractIndex = log.ContractIndex,
                        ContractSubIndex = log.ContractSubIndex,
                        TokenId = log.TokenId,
                        MetadataUrl = log.MetadataUrl,
                        HashHex = log.HashHex
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Parses CIS event from input bytes. returns null if the bytes do not represent standard CIS event.
        /// </summary>
        /// <param name="txnId">Transaction Id</param>
        /// <param name="address">CIS Contract Address</param>
        /// <param name="bytes">Input bytes</param>
        /// <returns></returns>
        private CisEvent? ParseCisEvent(
            long txnId,
            Concordium.Sdk.Types.ContractAddress address,
            byte[] bytes)
        {
            if (!CisEvent.IsCisEvent(bytes))
            {
                return null;
            }

            CisEvent cisEvent;
            if (!CisEvent.TryParse(bytes, address, out cisEvent))
            {
                return null;
            }

            return cisEvent;
        }
    }
}