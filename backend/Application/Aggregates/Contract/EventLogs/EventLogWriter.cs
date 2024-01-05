using System.Data.Common;
using System.Numerics;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Common.Diagnostics;
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
        int ApplyTokenUpdates(IEnumerable<CisEventTokenUpdate> tokenUpdates);
        /// <summary>
        /// Applies computed Account Updates to the database
        /// </summary>
        /// <param name="accountUpdates"></param>
        /// <returns>Total no of accounts updates applied to database</returns>
        int ApplyAccountUpdates(IList<CisAccountUpdate> accountUpdates);
        /// <summary>
        /// Store token events.
        /// </summary>
        void ApplyTokenEvents(IEnumerable<TokenEvent> tokenTransactions);
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
        public int ApplyTokenUpdates(IEnumerable<CisEventTokenUpdate> tokenUpdates)
        {
            using var counter = _metrics.MeasureDuration(nameof(EventLogWriter), nameof(ApplyTokenUpdates));

            using var context = _dbContextFactory.CreateDbContext();
            var connection = context.Database.GetDbConnection();

            connection.Open();
            var batch = connection.CreateBatch();

            foreach (var tokenUpdate in tokenUpdates)
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

            batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
            var updates = batch.ExecuteNonQuery();
            connection.Close();

            return updates;
        }

        private static DbBatchCommand CreateTokenAmountUpdateCmd(DbBatchCommand cmd, CisEventTokenAmountUpdate e)
        {
            cmd.CommandText = @"
                INSERT INTO graphql_tokens(contract_index, contract_sub_index, token_id, total_supply)
                VALUES (@ContractIndex, @ContractSubIndex, @TokenId, @AmountDelta)
                ON CONFLICT ON CONSTRAINT graphql_tokens_pkey
                DO UPDATE SET total_supply = graphql_tokens.total_supply + @AmountDelta";
            
            cmd.Parameters.Add(new NpgsqlParameter<long>("ContractIndex", Convert.ToInt64(e.ContractIndex)));
            cmd.Parameters.Add(new NpgsqlParameter<long>("ContractSubIndex", Convert.ToInt64(e.ContractSubIndex)));
            cmd.Parameters.Add(new NpgsqlParameter<string>("TokenId", e.TokenId));
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
        public int ApplyAccountUpdates(IList<CisAccountUpdate> accountUpdates)
        {
            var accountBaseAddresses = accountUpdates
                .Select(u => 
                    Concordium.Sdk.Types.AccountAddress.From(u.Address.AsString)
                    .GetBaseAddress()
                    .ToString())
                .Distinct();
            var accountsMap = this._accountLookup.GetAccountIdsFromBaseAddresses(accountBaseAddresses);
            using var counter = _metrics.MeasureDuration(nameof(EventLogWriter), nameof(ApplyAccountUpdates));

            using var context = _dbContextFactory.CreateDbContext();
            var connection = context.Database.GetDbConnection();

            connection.Open();
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
                    _logger.Debug("could not find account: {account}", accountUpdate.Address);
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

            batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
            var updates = batch.ExecuteNonQuery();
            connection.Close();

            return updates;
        }

        /// <inheritdoc/>
        public void ApplyTokenEvents(IEnumerable<TokenEvent> tokenTransactions)
        {
            using var context = _dbContextFactory.CreateDbContext();
            context.TokenEvents.AddRange(tokenTransactions);
            context.SaveChanges();
        }
    }
}
