using System.Reflection;
using Application.Common.FeatureFlags;
using DatabaseScripts;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Npgsql;

namespace Application.Database
{
    public class DatabaseMigrator
    {
        private const string MainDatabaseSqlScriptsFolder = "SqlScripts";
        private const string NodeCacheSqlScriptsFolder = "SqlScriptsNodeCache";
        private readonly DatabaseSettings _settings;
        private readonly IFeatureFlags _featureFlags;
        private readonly ILogger _logger;
        private readonly DbUpLogWrapper _dbUpLogWrapper;
        private readonly Assembly _sqlScriptsAssembly;

        public DatabaseMigrator(DatabaseSettings settings, IFeatureFlags featureFlags)
        {
            _settings = settings;
            _featureFlags = featureFlags;
            _logger = Log.ForContext(GetType());
            _dbUpLogWrapper = new DbUpLogWrapper(_logger);
            _sqlScriptsAssembly = typeof(DatabaseScriptsMarkerType).Assembly;
        }

        public void MigrateDatabases()
        {
            if (_featureFlags.MigrateDatabasesAtStartup)
            {
                _logger.Information("Starting database migration...");
                MigrateDatabase(_settings.ConnectionString, MainDatabaseSqlScriptsFolder);
                MigrateDatabase(_settings.ConnectionStringNodeCache, NodeCacheSqlScriptsFolder);
                _logger.Information("Database migration finished successfully");
            }
            else
            {
                _logger.Warning("Migration of databases is disabled. Will not check if any changed needs to be applied to databases!");
            }
        }
        private void MigrateDatabase(string connectionString, string sqlScriptsFolder)
        {
            EnsureDatabase.For.PostgresqlDatabase(connectionString, _dbUpLogWrapper);

            var upgrader = GetUpgrader(connectionString, sqlScriptsFolder);
            EnsureExecutedScriptsStillExist(upgrader);
            
            if (upgrader.IsUpgradeRequired())
            {
                EnsureScriptNamingConventionsFollowed(upgrader);

                _logger.Information("Running database migration scripts: ");
                
                var result = upgrader.PerformUpgrade();
                if (!result.Successful)
                {
                    if (result.Error != null)
                        throw new Exception($"Error executing database migration scripts (problematic script: {result.ErrorScript.Name})", result.Error);
                    throw new Exception($"Error executing database migration scripts (problematic script: {result.ErrorScript.Name})");
                }
            }
            else
            {
                _logger.Information("Database does not require upgrade.");
            }
        }

        public void EnsureScriptNamingConventionsFollowed()
        {
            var mainUpgrader = GetUpgrader(_settings.ConnectionString, MainDatabaseSqlScriptsFolder);
            EnsureScriptNamingConventionsFollowed(mainUpgrader);
            var nodeCacheUpgrader = GetUpgrader(_settings.ConnectionStringNodeCache, NodeCacheSqlScriptsFolder);
            EnsureScriptNamingConventionsFollowed(nodeCacheUpgrader);
        }

        private void EnsureScriptNamingConventionsFollowed(UpgradeEngine upgrader)
        {
            var discoveredScripts = upgrader.GetDiscoveredScripts();
            var validator = new DatabaseScriptsValidator();
            validator.EnsureScriptNamingConventionsFollowed(discoveredScripts);
        }

        private void EnsureExecutedScriptsStillExist(UpgradeEngine upgrader)
        {
            var executedButNotDiscoveredScripts = upgrader.GetExecutedButNotDiscoveredScripts();
            if (executedButNotDiscoveredScripts.Any())
                throw new DatabaseValidationException($"Already executed scripts are no longer present: {string.Join(", ", executedButNotDiscoveredScripts)}");
        }

        private UpgradeEngine GetUpgrader(string connectionString, string sqlScriptsFolder)
        {
            var upgrader = DeployChanges.To
                .PostgresqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(_sqlScriptsAssembly, scriptPath => scriptPath.EndsWith(".sql") && scriptPath.Contains($".{sqlScriptsFolder}."))
                .LogTo(_dbUpLogWrapper)
                .WithTransaction()
                .Build();
            return upgrader;
        }

        private class DbUpLogWrapper : IUpgradeLog
        {
            private readonly ILogger _logger;

            public DbUpLogWrapper(ILogger logger)
            {
                _logger = logger;
            }

            public void WriteInformation(string format, params object[] args)
            {
                _logger.Information(format, args);
            }

            public void WriteError(string format, params object[] args)
            {
                _logger.Information(format, args);
            }

            public void WriteWarning(string format, params object[] args)
            {
                _logger.Information(format, args);
            }
        }
    }
}