using Application.Api.GraphQL.Tokens;
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
        public List<CisAccountUpdate> HandleLogs(TransactionPair[] transactions)
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
                AccountUpdates = GetAccountUpdates(e),
                transactions = GetTransaction(e),
            });

            IEnumerable<CisEventTokenUpdate> tokenUpdates = updates
                .Where(u => u.TokenUpdate != null)
                .Select(u => u.TokenUpdate)
                .Cast<CisEventTokenUpdate>();
            var accountUpdates = updates.SelectMany(a => a.AccountUpdates).ToList();
            var tokenTransactions = updates.Select(u => u.transactions).Where(t => t != null).ToList();

            if (tokenUpdates.Count() > 0)
            {
                writer.ApplyTokenUpdates(tokenUpdates);
            }

            if (accountUpdates.Count() > 0)
            {
                writer.ApplyAccountUpdates(accountUpdates);
            }

            if (tokenTransactions.Count() > 0)
            {
                writer.ApplyTokenTransactions(tokenTransactions);
            }

            return accountUpdates;
        }

        private TokenTransaction? GetTransaction(CisEvent log)
        {
            switch (log)
            {
                case CisBurnEvent e:
                    return new TokenTransaction(
                        e.ContractIndex,
                        e.ContractSubIndex,
                        e.TokenId,
                        e.TransactionId,
                        new CisEventDataBurn
                        {
                            Amount = e.TokenAmount.ToString(),
                            From = Address.from(e.FromAddress),
                        });
                case CisMintEvent e:
                    return new TokenTransaction(
                        e.ContractIndex,
                        e.ContractSubIndex,
                        e.TokenId,
                        e.TransactionId,
                        new CisEventDataMint
                        {
                            Amount = e.TokenAmount.ToString(),
                            To = Address.from(e.ToAddress),
                        });
                case CisTransferEvent e:
                    return new TokenTransaction(
                        e.ContractIndex,
                        e.ContractSubIndex,
                        e.TokenId,
                        e.TransactionId,
                        new CisEventDataTransfer
                        {
                            Amount = e.TokenAmount.ToString(),
                            From = Address.from(e.FromAddress),
                            To = Address.from(e.ToAddress),
                        });
                case CisTokenMetadataEvent e:
                    return new TokenTransaction(
                        e.ContractIndex,
                        e.ContractSubIndex,
                        e.TokenId,
                        e.TransactionId,
                        new CisEventDataMetadataUpdate
                        {
                            MetadataUrl = e.MetadataUrl,
                            MetadataHashHex = e.HashHex,
                        });
                default:
                    return null;
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
                        AmountDelta = log.TokenAmount * -1,
                        TransactionId = log.TransactionId,
                    };
                case CisMintEvent log:
                     return new CisEventTokenAmountUpdate()
                    {
                        ContractIndex = log.ContractIndex,
                        ContractSubIndex = log.ContractSubIndex,
                        TokenId = log.TokenId,
                        AmountDelta = log.TokenAmount,
                        TransactionId = log.TransactionId,
                    };
                case CisTokenMetadataEvent log:
                    return new CisEventTokenMetadataUpdate()
                    {
                        ContractIndex = log.ContractIndex,
                        ContractSubIndex = log.ContractSubIndex,
                        TokenId = log.TokenId,
                        MetadataUrl = log.MetadataUrl,
                        HashHex = log.HashHex,
                        TransactionId = log.TransactionId,
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
            if (!CisEvent.TryParse(bytes, address, txnId, out cisEvent))
            {
                return null;
            }

            return cisEvent;
        }
    }
}