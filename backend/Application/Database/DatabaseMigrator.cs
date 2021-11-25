using System;
using System.Linq;
using System.Reflection;
using DatabaseScripts;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Npgsql;
using Serilog;

namespace Application.Database
{
    public class DatabaseMigrator
    {
        private readonly DatabaseSettings _settings;
        private readonly ILogger _logger;
        private readonly DbUpLogWrapper _dbUpLogWrapper;
        private readonly Assembly _sqlScriptsAssembly;

        public DatabaseMigrator(DatabaseSettings settings)
        {
            _settings = settings;
            _logger = Log.ForContext(GetType());
            _dbUpLogWrapper = new DbUpLogWrapper(_logger);
            _sqlScriptsAssembly = typeof(DatabaseScriptsMarkerType).Assembly;
        }

        public void MigrateDatabase()
        {
            EnsureDatabase.For.PostgresqlDatabase(_settings.ConnectionString, _dbUpLogWrapper);

            var upgrader = GetUpgrader();
            EnsureExecutedScriptsStillExist(upgrader);
            
            if (upgrader.IsUpgradeRequired())
            {
                EnsureScriptNamingConventionsFollowed(upgrader);

                _logger.Information("Running database migration scripts: ");
                
                var result = upgrader.PerformUpgrade();
                if (!result.Successful)
                {
                    if (result.Error != null)
                        throw new Exception($"Error executing database migration scripts (problematic script: {result.ErrorScript})", result.Error);
                    throw new Exception($"Error executing database migration scripts (problematic script: {result.ErrorScript})");
                }
            }
            else
            {
                _logger.Information("Database does not required upgrade.");
            }
        }

        public void EnsureDatabaseMigrationNotNeeded()
        {
            _logger.Information("Ensuring that database exists and that it is fully upgraded...");
            
            var upgrader = GetUpgrader();
            bool isUpgradeRequired;
            try
            {
                isUpgradeRequired = upgrader.IsUpgradeRequired();
                if (isUpgradeRequired)
                    _logger.Warning("Database exists but requires upgrading. Run the application in database migration mode to upgrade database.");
                else
                    _logger.Information("Database exists and is fully upgraded.");
            }
            catch (PostgresException e)
            {
                if (e.SqlState == "3D000") 
                {
                    _logger.Warning("Database does not exist. Run the application in database migration mode to create and upgrade database.");
                    isUpgradeRequired = true;
                }
                else
                    throw;
            }
            
            if (isUpgradeRequired)
                throw new DatabaseValidationException("Database upgrade required!");
        }

        public void EnsureScriptNamingConventionsFollowed()
        {
            var upgrader = GetUpgrader();
            EnsureScriptNamingConventionsFollowed(upgrader);
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

        private UpgradeEngine GetUpgrader()
        {
            var upgrader = DeployChanges.To
                .PostgresqlDatabase(_settings.ConnectionString)
                .WithScriptsEmbeddedInAssembly(_sqlScriptsAssembly, scriptPath => scriptPath.EndsWith(".sql") && scriptPath.Contains(".SqlScripts."))
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