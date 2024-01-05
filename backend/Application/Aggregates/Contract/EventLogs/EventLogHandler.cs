using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.Import;
using ContractEvent = Concordium.Sdk.Types.ContractEvent;

namespace Application.Aggregates.Contract.EventLogs
{

    public class EventLogHandler
    {
        public EventLogWriter writer;

        public EventLogHandler(EventLogWriter logWriter)
        {
            writer = logWriter;
        }

        private readonly record struct PossibleCis2Event(long TransactionId, Concordium.Sdk.Types.ContractAddress Address, ContractEvent ContractEvent);

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
                .Select(e => ParseCisEvent(e.Address, e.TransactionId, e.ContractEvent.Bytes))
                .Where(e => e != null)
                .Cast<CisEvent>()
                .ToList();

            //Handle Smart Contract Cis Events Logs
            var updates = cisEvents.Select(e => new
            {
                TokenUpdate = GetTokenUpdates(e),
                AccountUpdates = GetAccountUpdates(e),
                TokenEvents = e.GetTokenEvent()
            }).ToList();

            var tokenUpdates = updates
                .Where(u => u.TokenUpdate != null)
                .Select(u => u.TokenUpdate)
                .Cast<CisEventTokenUpdate>()
                .ToList();
            
            var accountUpdates = updates.SelectMany(a => a.AccountUpdates).ToList();

            var tokenEvents = updates.Select(t => t.TokenEvents)
                .Where(t => t != null)
                .Cast<TokenEvent>()
                .ToList();

            if (tokenUpdates.Any())
            {
                writer.ApplyTokenUpdates(tokenUpdates);
            }

            if (accountUpdates.Any())
            {
                writer.ApplyAccountUpdates(accountUpdates);
            }

            if (tokenEvents.Any())
            {
                writer.ApplyTokenEvents(tokenEvents);
            }

            return accountUpdates;
        }

        /// <summary>
        /// Computes Token amount changes for an Account.
        /// </summary>
        /// <param name="log"></param>
        /// <returns>Parsed <see cref="CisAccountUpdate"/></returns>
        private static CisAccountUpdate[] GetAccountUpdates(CisEvent log)
        {
            switch (log)
            {
                case CisBurnEvent e:
                    if (e.FromAddress is Application.Api.GraphQL.Accounts.AccountAddress accntAddress)
                    {
                        return new[] {
                            new CisAccountUpdate
                            {
                                ContractIndex = e.ContractIndex,
                                ContractSubIndex = e.ContractSubIndex,
                                TokenId = e.TokenId,
                                AmountDelta = e.TokenAmount * -1,
                                Address = accntAddress
                            }
                        };
                    }

                    return new CisAccountUpdate[] { };
                case CisTransferEvent e:
                    var ret = new List<CisAccountUpdate>();
                    if (e.FromAddress is Application.Api.GraphQL.Accounts.AccountAddress accntAddress2)
                    {
                        ret.Add(new CisAccountUpdate()
                        {
                            ContractIndex = e.ContractIndex,
                            ContractSubIndex = e.ContractSubIndex,
                            TokenId = e.TokenId,
                            AmountDelta = e.TokenAmount * -1,
                            Address = accntAddress2
                        });
                    }

                    if (e.ToAddress is Application.Api.GraphQL.Accounts.AccountAddress accntAddress3)
                    {
                        ret.Add(new CisAccountUpdate()
                        {
                            ContractIndex = e.ContractIndex,
                            ContractSubIndex = e.ContractSubIndex,
                            TokenId = e.TokenId,
                            AmountDelta = e.TokenAmount,
                            Address = accntAddress3
                        });
                    }

                    return ret.ToArray();
                case CisMintEvent e:
                    if (e.ToAddress is Application.Api.GraphQL.Accounts.AccountAddress accntAddress4)
                    {
                        return new[] {new CisAccountUpdate()
                        {
                            ContractIndex = e.ContractIndex,
                            ContractSubIndex = e.ContractSubIndex,
                            TokenId = e.TokenId,
                            AmountDelta = e.TokenAmount,
                            Address = accntAddress4
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
        private static CisEventTokenUpdate? GetTokenUpdates(CisEvent cisLog)
        {
            return cisLog switch
            {
                CisBurnEvent log => new CisEventTokenAmountUpdate()
                {
                    ContractIndex = log.ContractIndex,
                    ContractSubIndex = log.ContractSubIndex,
                    TokenId = log.TokenId,
                    AmountDelta = log.TokenAmount * -1
                },
                CisMintEvent log => new CisEventTokenAmountUpdate()
                {
                    ContractIndex = log.ContractIndex,
                    ContractSubIndex = log.ContractSubIndex,
                    TokenId = log.TokenId,
                    AmountDelta = log.TokenAmount
                },
                CisTokenMetadataEvent log => new CisEventTokenMetadataUpdate()
                {
                    ContractIndex = log.ContractIndex,
                    ContractSubIndex = log.ContractSubIndex,
                    TokenId = log.TokenId,
                    MetadataUrl = log.MetadataUrl,
                    HashHex = log.HashHex
                },
                _ => null
            };
        }

        /// <summary>
        /// Parses CIS event from input bytes. returns null if the bytes do not represent standard CIS event.
        /// </summary>
        /// <param name="address">CIS Contract Address</param>
        /// <param name="txnId">Transaction Id</param>
        /// <param name="bytes">Input bytes</param>
        /// <returns></returns>
        private static CisEvent? ParseCisEvent(
            Concordium.Sdk.Types.ContractAddress address,
            long txnId,
            byte[] bytes)
        {
            CisEvent cisEvent;
            if (!CisEvent.TryParse(bytes, address, txnId, out cisEvent))
            {
                return null;
            }

            return cisEvent;
        }
    }
}
