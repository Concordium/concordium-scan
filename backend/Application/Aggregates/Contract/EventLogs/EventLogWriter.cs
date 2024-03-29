using System.Data.Common;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Import;
using Application.Common.Diagnostics;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Aggregates.Contract.EventLogs
{
    public interface IEventLogWriter
    {
        /// <summary>
        /// Applies computed token updates to the database
        /// </summary>
        /// <param name="tokenUpdates">Computed Token Updates</param>
        /// <returns>Total no of token updates applied to database</returns>
        Task<int> ApplyTokenUpdates(IEnumerable<CisEventTokenUpdate> tokenUpdates);
        /// <summary>
        /// Applies computed Account Updates to the database
        /// </summary>
        /// <param name="accountUpdates"></param>
        /// <returns>Total no of accounts updates applied to database</returns>
        Task<int> ApplyAccountUpdates(IList<CisAccountUpdate> accountUpdates);
        /// <summary>
        /// Store token events.
        /// </summary>
        Task ApplyTokenEvents(IList<TokenEvent> tokenEvents);
    }
    
    /// <summary>
    /// Applies computed updates to the Database
    /// </summary>
    public class EventLogWriter : IEventLogWriter
    {
        private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
        private readonly IMetrics _metrics;
        private readonly IAccountLookup _accountLookup;
        private readonly ILogger _logger;

        public EventLogWriter(
            IDbContextFactory<GraphQlDbContext> dbContextFactory,
            IAccountLookup accountLookup,
            IMetrics metrics)
        {
            _dbContextFactory = dbContextFactory;
            _metrics = metrics;
            _accountLookup = accountLookup;
            _logger = Log.ForContext(GetType());

        }

        /// <inheritdoc/>
        public async Task<int> ApplyTokenUpdates(IEnumerable<CisEventTokenUpdate> tokenUpdates)
        {
            using var counter = _metrics.MeasureDuration(nameof(EventLogWriter), nameof(ApplyTokenUpdates));

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            await connection.OpenAsync();
            var batch = connection.CreateBatch();

            var cisEventTokenUpdates = tokenUpdates.ToList();
            foreach (var tokenUpdate in cisEventTokenUpdates)
            {
                var cmd = batch.CreateBatchCommand();

                switch (tokenUpdate)
                {
                    case CisEventTokenAmountUpdate e:
                        batch.BatchCommands.Add(CreateTokenAmountUpdateCmd(cmd, e));
                        break;
                    case CisEventTokenMetadataUpdate e:
                        batch.BatchCommands.Add(CreateTokenMetadataUpdatedCmd(cmd, e));
                        break;
                    default: continue;
                }
            }

            await batch.PrepareAsync(); // Preparing will speed up the updates, particularly when there are many!
            var updates = await batch.ExecuteNonQueryAsync();
            await connection.CloseAsync();

            return updates;
        }

        private static DbBatchCommand CreateTokenAmountUpdateCmd(DbBatchCommand cmd, CisEventTokenAmountUpdate e)
        {
            cmd.CommandText = @"
                INSERT INTO graphql_tokens(contract_index, contract_sub_index, token_id, token_address, total_supply)
                VALUES (@ContractIndex, @ContractSubIndex, @TokenId, @TokenAddress, @AmountDelta)
                ON CONFLICT ON CONSTRAINT graphql_tokens_pkey
                DO UPDATE SET total_supply = graphql_tokens.total_supply + @AmountDelta";
            
            cmd.Parameters.Add(new NpgsqlParameter<long>("ContractIndex", Convert.ToInt64(e.ContractIndex)));
            cmd.Parameters.Add(new NpgsqlParameter<long>("ContractSubIndex", Convert.ToInt64(e.ContractSubIndex)));
            cmd.Parameters.Add(new NpgsqlParameter<string>("TokenId", e.TokenId));
            cmd.Parameters.Add(new NpgsqlParameter<string>("TokenAddress", Token.EncodeTokenAddress(e.ContractIndex, e.ContractSubIndex, e.TokenId)));
            cmd.Parameters.Add(new NpgsqlParameter<BigInteger>("AmountDelta", e.AmountDelta));

            return cmd;
        }

        private static DbBatchCommand CreateTokenMetadataUpdatedCmd(DbBatchCommand cmd, CisEventTokenMetadataUpdate e)
        {
            cmd.CommandText = @"
                UPDATE graphql_tokens 
                SET metadata_url = @MetadataUrl 
                WHERE contract_index = @ContractIndex 
                    AND contract_sub_index = @ContractSubIndex 
                    AND token_id = @TokenId";

            cmd.Parameters.Add(new NpgsqlParameter<long>("ContractIndex", Convert.ToInt64(e.ContractIndex)));
            cmd.Parameters.Add(new NpgsqlParameter<long>("ContractSubIndex", Convert.ToInt64(e.ContractSubIndex)));
            cmd.Parameters.Add(new NpgsqlParameter<string>("TokenId", e.TokenId));
            cmd.Parameters.Add(new NpgsqlParameter("MetadataUrl", e.MetadataUrl));

            return cmd;
        }

        /// <inheritdoc/>
        public async Task<int> ApplyAccountUpdates(IList<CisAccountUpdate> accountUpdates)
        {
            var accountBaseAddresses = accountUpdates
                .Select(u => 
                    Concordium.Sdk.Types.AccountAddress.From(u.Address.AsString)
                    .GetBaseAddress()
                    .ToString())
                .Distinct();
            var accountsMap = _accountLookup.GetAccountIdsFromBaseAddresses(accountBaseAddresses);
            using var counter = _metrics.MeasureDuration(nameof(EventLogWriter), nameof(ApplyAccountUpdates));

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            await connection.OpenAsync();
            var batch = connection.CreateBatch();
            foreach (var accountUpdate in accountUpdates)
            {
                if (accountUpdate is null)
                {
                    continue;
                }

                var accountBaseAddress = Concordium.Sdk.Types.AccountAddress.From(accountUpdate.Address.AsString)
                    .GetBaseAddress()
                    .ToString();
                if (accountsMap[accountBaseAddress] is null 
                    || !accountsMap[accountBaseAddress].HasValue)
                {
                    _logger.Debug("could not find account: {account}", accountUpdate.Address.AsString);
                    continue;
                }

                var cmd = batch.CreateBatchCommand();
                cmd.CommandText = @"INSERT INTO graphql_account_tokens (contract_index, contract_sub_index, token_id, account_id, balance)
                                    VALUES(@ContractIndex, @ContractSubIndex, @TokenId, @AccountId, @AmountDelta)
                                    ON CONFLICT ON CONSTRAINT graphql_account_tokens_pk
                                    DO UPDATE SET balance = graphql_account_tokens.balance + @AmountDelta";
                cmd.Parameters.Add(new NpgsqlParameter<long>("ContractIndex", Convert.ToInt64(accountUpdate.ContractIndex)));
                cmd.Parameters.Add(new NpgsqlParameter<long>("ContractSubIndex", Convert.ToInt64(accountUpdate.ContractSubIndex)));
                cmd.Parameters.Add(new NpgsqlParameter<string>("TokenId", accountUpdate.TokenId));
                cmd.Parameters.Add(new NpgsqlParameter<BigInteger>("AmountDelta", accountUpdate.AmountDelta));
                cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", accountsMap[accountBaseAddress].Value));
                batch.BatchCommands.Add(cmd);
            }

            await batch.PrepareAsync(); // Preparing will speed up the updates, particularly when there are many!
            var updates = await batch.ExecuteNonQueryAsync();
            await connection.CloseAsync();

            return updates;
        }

        /// <inheritdoc/>
        public async Task ApplyTokenEvents(IList<TokenEvent> tokenEvents)
        {
            if (tokenEvents.Count == 0)
            {
                return;
            }
            const string startSql =
                "insert into graphql_token_events (contract_address_index, contract_address_subindex, token_id, event) values ";
            var stringBuilder = new StringBuilder(startSql);
            foreach (var tokenEvent in tokenEvents)
            {
                stringBuilder.Append(
                    $"({(long)tokenEvent.ContractIndex}, {tokenEvent.ContractSubIndex}, '{tokenEvent.TokenId}', '{System.Text.Json.JsonSerializer.Serialize(tokenEvent.Event, EfCoreJsonSerializerOptionsFactory.Default)}'),");
            }

            stringBuilder.Remove(stringBuilder.Length - 1, 1); // Remove final ','
            stringBuilder.Append(';');
            var query = stringBuilder.ToString();

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Database.GetDbConnection().ExecuteAsync(query);
        }
    }
}
