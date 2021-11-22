using System.Reflection;
using DbUp;
using DbUp.Engine;
using Npgsql;

namespace Application.Database
{
    public class DatabaseMigrator
    {
        private readonly DatabaseSettings _settings;
        private readonly Assembly _sqlScriptsAssembly;

        public DatabaseMigrator(DatabaseSettings settings, Assembly sqlScriptsAssembly)
        {
            _settings = settings;
            _sqlScriptsAssembly = sqlScriptsAssembly;
        }

        public void MigrateDatabase()
        {
            EnsureDatabase.For.PostgresqlDatabase(_settings.ConnectionString);

            var upgrader = GetUpgrader();
            EnsureExecutedScriptsStillExist(upgrader);
            
            if (upgrader.IsUpgradeRequired())
            {
                EnsureScriptNamingConventionsFollowed(upgrader);

                // TODO: Logging....
                Console.WriteLine("Running database migration scripts: ");
                
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
                // TODO: Logging....
                Console.WriteLine("No database migration scripts pending");
            }
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
                .LogToConsole() // TODO: Logging.... Consider where logging should go!
                .WithTransaction()
                .Build();
            return upgrader;
        }

        public void EnsureDatabaseMigrationNotNeeded()
        {
            var upgrader = GetUpgrader();

            bool isUpgradeRequired;
            try
            {
                isUpgradeRequired = upgrader.IsUpgradeRequired();
            }
            catch (PostgresException e)
            {
                if (e.SqlState == "3D000") // database does not exist!
                    isUpgradeRequired = true;
                else
                    throw;
            }
            
            if (isUpgradeRequired)
                throw new DatabaseValidationException("Database upgrade required!");
        }
    }
}