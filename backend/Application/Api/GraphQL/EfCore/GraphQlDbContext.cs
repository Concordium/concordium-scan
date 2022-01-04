using System.Linq.Expressions;
using Application.Database;
using ConcordiumSdk.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Application.Api.GraphQL.EfCore;

public class GraphQlDbContext : DbContext
{
    private readonly DatabaseSettings _settings;
    private readonly ILoggerFactory _loggerFactory;

    public DbSet<Block> Blocks { get; private set; }
    public DbSet<Transaction> Transactions { get; private set; }

    public GraphQlDbContext(DatabaseSettings settings, ILoggerFactory loggerFactory)
    {
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(_settings.ConnectionString)
            .UseLoggerFactory(_loggerFactory);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var timestampAsDateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTime>(
            dto => dto.DateTime,
            dt => dt);
        
        var blockHashAsStringConverter = new ValueConverter<string, byte[]>(
            str => new BlockHash(str).AsBytes,
            bytes => new BlockHash(bytes).AsString);
        
        var transactionHashAsStringConverter = new ValueConverter<string, byte[]>(
            str => new TransactionHash(str).AsBytes,
            bytes => new TransactionHash(bytes).AsString);
        
        var accountAddressAsStringConverter = new ValueConverter<string, byte[]>(
            str => new AccountAddress(str).AsBytes,
            bytes => new AccountAddress(bytes).AsString);

        var blockBuilder = modelBuilder.Entity<Block>()
            .ToTable("block");

        blockBuilder.Property(b => b.Id).HasColumnName("id").IsRequired();
        blockBuilder.Property(b => b.BlockHash).HasColumnName("block_hash").HasConversion(blockHashAsStringConverter).IsRequired();
        blockBuilder.Property(b => b.BlockHeight).HasColumnName("block_height").IsRequired();
        blockBuilder.Property(b => b.BlockSlotTime).HasColumnName("block_slot_time").HasConversion(timestampAsDateTimeOffsetConverter).IsRequired();
        blockBuilder.Property(b => b.BakerId).HasColumnName("block_baker");
        blockBuilder.Property(b => b.Finalized).HasColumnName("finalized");
        blockBuilder.Property(b => b.TransactionCount).HasColumnName("transaction_count");
        blockBuilder.OwnsOne(block => block.SpecialEvents,
            specialEventsBuilder =>
            {
                specialEventsBuilder.OwnsOne(x => x.Mint,
                    builder =>
                    {
                        builder.Property(m => m.BakingReward).HasColumnName("mint_baking_reward");
                        builder.Property(m => m.FinalizationReward).HasColumnName("mint_finalization_reward");
                        builder.Property(m => m.PlatformDevelopmentCharge).HasColumnName("mint_platform_development_charge");
                        builder.Property(m => m.FoundationAccount).HasColumnName("mint_foundation_account").HasConversion(accountAddressAsStringConverter);
                    });
                specialEventsBuilder.OwnsOne(x => x.FinalizationRewards,
                    builder =>
                    {
                        builder.Property(f => f.Remainder).HasColumnName("finalization_reward_remainder");
                        builder.OwnsMany(g => g.Rewards, rewardBuilder =>
                        {
                            rewardBuilder.ToTable("finalization_reward");
                            rewardBuilder.WithOwner().HasForeignKey("block_id");
                            rewardBuilder.Property<int>("index");
                            rewardBuilder.Property(d => d.Address).HasColumnName("address").HasConversion(accountAddressAsStringConverter);
                            rewardBuilder.Property(d => d.Amount).HasColumnName("amount");
                            rewardBuilder.HasKey("block_id", "index");
                        });
                    });
                specialEventsBuilder.OwnsOne(x => x.BlockRewards,
                    builder =>
                    {
                        builder.Property(x => x.TransactionFees).HasColumnName("block_reward_transaction_fees");
                        builder.Property(x => x.OldGasAccount).HasColumnName("block_reward_old_gas_account");
                        builder.Property(x => x.NewGasAccount).HasColumnName("block_reward_new_gas_account");
                        builder.Property(x => x.BakerReward).HasColumnName("block_reward_baker_reward");
                        builder.Property(x => x.FoundationCharge).HasColumnName("block_reward_foundation_charge");
                        builder.Property(x => x.BakerAccountAddress).HasColumnName("block_reward_baker_address").HasConversion(accountAddressAsStringConverter);
                        builder.Property(x => x.FoundationAccountAddress).HasColumnName("block_reward_foundation_account").HasConversion(accountAddressAsStringConverter);
                    });
                specialEventsBuilder.OwnsOne(x => x.BakingRewards,
                    builder =>
                    {
                        builder.Property(f => f.Remainder).HasColumnName("baking_reward_remainder");
                        builder.OwnsMany(g => g.Rewards, rewardBuilder =>
                        {
                            rewardBuilder.ToTable("baking_reward");
                            rewardBuilder.WithOwner().HasForeignKey("block_id");
                            rewardBuilder.Property<int>("index");
                            rewardBuilder.Property(d => d.Address).HasColumnName("address").HasConversion(accountAddressAsStringConverter);
                            rewardBuilder.Property(d => d.Amount).HasColumnName("amount");
                            rewardBuilder.HasKey("block_id", "index");
                        });
                    });
            });

        var transactionBuilder = modelBuilder.Entity<Transaction>()
            .ToTable("transaction_summary");
        
        transactionBuilder.Property(b => b.Id).HasColumnName("id").IsRequired();
        transactionBuilder.Property(b => b.BlockId).HasColumnName("block_id").IsRequired();
        transactionBuilder.Property(b => b.BlockHash).HasColumnName("block_hash").HasConversion(blockHashAsStringConverter).IsRequired();
        transactionBuilder.Property(b => b.BlockHeight).HasColumnName("block_height").IsRequired();
        transactionBuilder.Property(b => b.TransactionIndex).HasColumnName("transaction_index").IsRequired();
        transactionBuilder.Property(b => b.TransactionHash).HasColumnName("transaction_hash").HasConversion(transactionHashAsStringConverter).IsRequired();
        transactionBuilder.Property(b => b.SenderAccountAddress).HasColumnName("sender").HasConversion(accountAddressAsStringConverter).IsRequired();
        transactionBuilder.Property(b => b.CcdCost).HasColumnName("cost").IsRequired();
        transactionBuilder.Property(b => b.EnergyCost).HasColumnName("energy_cost").IsRequired();
    }
}
