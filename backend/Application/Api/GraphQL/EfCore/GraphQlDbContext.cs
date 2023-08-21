using Application.Aggregates.SmartContract;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.PassiveDelegations;
using Application.Api.GraphQL.Payday;
using Application.Api.GraphQL.Tokens;
using Application.Api.GraphQL.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class GraphQlDbContext : DbContext
{
    public DbSet<ImportState> ImportState { get; private set; }
    public DbSet<ChainParameters> ChainParameters { get; private set; }
    public DbSet<Block> Blocks { get; private set; }
    public DbSet<BlockRelated<FinalizationSummaryParty>> FinalizationSummaryFinalizers { get; private set; }
    public DbSet<SpecialEvent> SpecialEvents { get; private set; }
    public DbSet<Transaction> Transactions { get; private set; }
    public DbSet<Account> Accounts { get; private set; }

    /// <summary>
    /// Mapping of Accounts to CIS Tokens.
    /// </summary>
    public DbSet<AccountToken> AccountTokens { get; private set; }
    
    /// <summary>
    /// Parsed CIS Tokens.
    /// </summary>
    public DbSet<Token> Tokens { get; private set; }
    public DbSet<TransactionRelated<TransactionResultEvent>> TransactionResultEvents { get; private set; }
    public DbSet<AccountTransactionRelation> AccountTransactionRelations { get; private set; }
    public DbSet<AccountReleaseScheduleItem> AccountReleaseScheduleItems { get; private set; }
    public DbSet<IdentityProvider> IdentityProviders { get; private set; }
    public DbSet<AccountReward> AccountRewards { get; private set; }
    public DbSet<PaydayPoolReward> PaydayPoolRewards { get; private set; }
    public DbSet<AccountStatementEntry> AccountStatementEntries { get; private set; }
    public DbSet<Baker> Bakers { get; private set; }
    public DbSet<BakerTransactionRelation> BakerTransactionRelations { get; private set; }
    public DbSet<PassiveDelegation> PassiveDelegations { get; private set; }
    public DbSet<PaydayStatus> PaydayStatuses { get; private set; }
    public DbSet<PaydaySummary> PaydaySummaries { get; private set; }
    public DbSet<PoolPaydayStakes> PoolPaydayStakes { get; private set; }
    /// <summary>
    /// TODO
    /// </summary>
    public DbSet<SmartContract> SmartContract { get; private set; }
    /// <summary>
    /// TODO
    /// </summary>
    public DbSet<SmartContractEvent> SmartContractEvents { get; private set; }
    /// <summary>
    /// TODO
    /// </summary>
    public DbSet<ModuleReferenceSmartContractLinkEvent> ModuleReferenceSmartContractLinkEvents { get; private set; }
    /// <summary>
    /// TODO
    /// </summary>
    public DbSet<ModuleReferenceEvent> ModuleReferenceEvents { get; private set; }
    /// <summary>
    /// TODO
    /// </summary>
    public DbSet<SmartContractReadHeight> SmartContractReadHeights { get; private set; }

    public GraphQlDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
