using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Application.Api.GraphQL.Import.EventLogs
{

    public class EventLogHandler
    {
        public EventLogWriter writer;

        public EventLogHandler(EventLogWriter logWriter)
        {
            this.writer = logWriter;
        }

        /// <summary>
        /// Fetches log bytes from Transaction, parses them and persists them to the database.
        /// </summary>
        /// <param name="transactions">Pair of Database Persisted Transaction and on chain transaction</param>
        public void HandleLogs(TransactionPair[] transactions)
        {
            var events = transactions
                .Where(t => t.Source.Type.Kind == ConcordiumSdk.Types.BlockItemKind.AccountTransactionKind)
                .Where(t => t.Source.Result is TransactionSuccessResult && t != null)
                .Select(t => new { Id = t.Target.Id, Result = t.Source.Result as TransactionSuccessResult })
                .SelectMany(t => t.Result?.Events?.Select(e => new { TxnId = t.Id, Event = e }))
                .Select(e =>
                {
                    switch (e.Event)
                    {
                        case Updated evnt:
                            return new { e.TxnId, evnt.Address, evnt.Events };
                        case ContractInitialized evnt:
                            return new { e.TxnId, evnt.Address, evnt.Events };
                        case Interrupted evnt:
                            return new { e.TxnId, evnt.Address, evnt.Events };
                        default:
                            return new
                            {
                                e.TxnId,
                                Address = new ConcordiumSdk.Types.ContractAddress(0, 0),
                                Events = new BinaryData[0]
                            };
                    }
                })
                .SelectMany(e => e.Events.Select(evnt => new { e.TxnId, e.Address, Bytes = evnt.AsBytes }));

            //Select Cis Events
            var cisEvents = events
                .Select(e => ParseCisEvent(e.TxnId, e.Address, e.Bytes))
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
            var accountUpdates = updates.SelectMany(a => a.AccountUpdates);

            if (tokenUpdates.Count() > 0)
            {
                writer.ApplyTokenUpdates(tokenUpdates);
            }

            if (accountUpdates.Count() > 0)
            {
                writer.ApplyAccountUpdates(accountUpdates);
            }
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
            ConcordiumSdk.Types.ContractAddress address,
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