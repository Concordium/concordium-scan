using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Transactions;
using ContractInitialized = Application.Api.GraphQL.Transactions.ContractInitialized;

namespace Application.Aggregates.Contract.EventLogs
{

    public class EventLogHandler
    {
        public EventLogWriter writer;

        public EventLogHandler(EventLogWriter logWriter)
        {
            writer = logWriter;
        }

        /// <summary>
        /// Fetches log bytes from Transaction, parses them and persists them to the database.
        /// </summary>
        public List<CisAccountUpdate> HandleLogs(IContractRepository contractRepository)
        {
            var addedEvents = contractRepository.GetContractEventsAddedInTransaction()
                .OrderBy(ce => ce.BlockHeight)
                .ThenBy(ce => ce.TransactionIndex)
                .ThenBy(ce => ce.EventIndex);

            var possibleCis2Events = addedEvents
                .SelectMany(PossibleCis2Event.ToIter)
                .ToList();
            

            //Select Cis Events
            var cisEvents = possibleCis2Events
                .Select(e => ParseCisEvent(e.Address, e.TransactionHash, e.EventBytes, e.EventParsed))
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
        
        private readonly record struct PossibleCis2Event(
            string TransactionHash,
            ContractAddress Address,
            byte[] EventBytes,
            string? EventParsed)
        {
            internal static IEnumerable<PossibleCis2Event> ToIter(ContractEvent contractEvent)
            {
                var transactionHash = contractEvent.TransactionHash;
                var contractAddress = new ContractAddress(contractEvent.ContractAddressIndex,
                    contractEvent.ContractAddressSubIndex);
                
                return contractEvent.Event switch
                {
                    ContractInitialized contractInitialized => MapEvents(contractInitialized.EventsAsHex,
                        contractInitialized.Events, transactionHash, contractAddress),
                    ContractInterrupted contractInterrupted => MapEvents(contractInterrupted.EventsAsHex,
                        contractInterrupted.Events, transactionHash, contractAddress),
                    ContractUpdated contractUpdated => MapEvents(contractUpdated.EventsAsHex, contractUpdated.Events,
                        transactionHash, contractAddress),
                    _ => Enumerable.Empty<PossibleCis2Event>()
                };
            }
            
            private static IEnumerable<PossibleCis2Event> MapEvents(
                IReadOnlyList<string> hexEvents,
                IReadOnlyList<string>? parsedEvents,
                string transactionHash,
                ContractAddress contractAddress)
            {
                for (var i = 0; i < hexEvents.Count; i++)
                {
                    var parsedEvent = parsedEvents != null && i < parsedEvents.Count ?
                        parsedEvents[i] : null;
                    yield return new PossibleCis2Event(
                        transactionHash,
                        contractAddress,
                        Convert.FromHexString(hexEvents[i]),
                        parsedEvent
                    );
                }   
            }
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
        /// <param name="transactionHash">Transaction Hash</param>
        /// <param name="bytes">Input bytes</param>
        /// <param name="parsed">Parsed event in human interpretable form.</param>
        /// <returns></returns>
        private static CisEvent? ParseCisEvent(
            ContractAddress address,
            string transactionHash,
            byte[] bytes,
            string? parsed)
        {
            _ = CisEvent.TryParse(bytes, address, transactionHash, parsed, out var cisEvent);
            return cisEvent;
        }
    }
}
