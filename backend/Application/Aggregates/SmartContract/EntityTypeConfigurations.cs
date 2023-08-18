using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.SmartContract;

public sealed class
    SmartContractReadHeightEntityTypeConfigurations : IEntityTypeConfiguration<
        SmartContractReadHeight>
{
    public void Configure(EntityTypeBuilder<SmartContractReadHeight> builder)
    {
        builder.ToTable("graphql_smart_contract_read_heights");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}

public sealed class SmartContractEntityTypeConfigurations : IEntityTypeConfiguration<SmartContract>
{
    public void Configure(EntityTypeBuilder<SmartContract> builder)
    {
        builder.ToTable("graphql_smart_contracts");
        builder.HasKey(x => new
        {
            x.BlockHeight, 
            x.TransactionIndex,
            x.EventIndex,
            x.ContractAddressIndex,
            x.ContractAddressSubIndex
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.TransactionHash)
            .HasColumnName("transaction_hash");
        builder.Property(x => x.TransactionIndex)
            .HasColumnName("transaction_index");
        builder.Property(x => x.EventIndex)
            .HasColumnName("event_index");
        builder.Property(x => x.ContractAddressIndex)
            .HasColumnName("contract_address_index");
        builder.Property(x => x.ContractAddressSubIndex)
            .HasColumnName("contract_address_sub_index");
        builder.Property(x => x.Creator)
            .HasColumnName("creator")
            .HasConversion<AccountAddressConverter>();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}

public sealed class SmartContractEventEntityTypeConfigurations : IEntityTypeConfiguration<SmartContractEvent>
{
    public void Configure(EntityTypeBuilder<SmartContractEvent> builder)
    {
        builder.ToTable("graphql_smart_contract_events");
        builder.HasKey(x => new
        {
            x.BlockHeight, 
            x.TransactionIndex,
            x.EventIndex,
            x.ContractAddressIndex,
            x.ContractAddressSubIndex
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.TransactionHash)
            .HasColumnName("transaction_hash");
        builder.Property(x => x.TransactionIndex)
            .HasColumnName("transaction_index");
        builder.Property(x => x.EventIndex)
            .HasColumnName("event_index");
        builder.Property(x => x.ContractAddressIndex)
            .HasColumnName("contract_address_index");
        builder.Property(x => x.ContractAddressSubIndex)
            .HasColumnName("contract_address_sub_index");
        builder.Property(x => x.Event)
            .HasColumnName("event")
            .HasColumnType("json")
            .HasConversion<TransactionResultEventToJsonConverter>();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}

public sealed class ModuleReferenceEventEntityTypeConfigurations : IEntityTypeConfiguration<ModuleReferenceEvent>
{
    public void Configure(EntityTypeBuilder<ModuleReferenceEvent> builder)
    {
        builder.ToTable("graphql_module_reference_events");
        builder.HasKey(x => new
        {
            x.BlockHeight,
            x.TransactionIndex,
            x.EventIndex,
            x.ModuleReference
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.TransactionHash)
            .HasColumnName("transaction_hash");
        builder.Property(x => x.TransactionIndex)
            .HasColumnName("transaction_index");
        // TODO - not needed, but suggest to keep to have common filtering on aggregate
        builder.Property(x => x.EventIndex)
            .HasColumnName("event_index");
        builder.Property(x => x.ModuleReference)
            .HasColumnName("module_reference");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}

public sealed class ModuleReferenceSmartContractLinkEventEntityTypeConfigurations : IEntityTypeConfiguration<ModuleReferenceSmartContractLinkEvent>
{
    public void Configure(EntityTypeBuilder<ModuleReferenceSmartContractLinkEvent> builder)
    {
        builder.ToTable("graphql_module_reference_smart_contract_link_events");
        builder.HasKey(x => new
        {
            x.BlockHeight,
            x.TransactionIndex,
            x.EventIndex,
            x.ModuleReference,
            x.ContractAddressIndex,
            x.ContractAddressSubIndex
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.TransactionHash)
            .HasColumnName("transaction_hash");
        builder.Property(x => x.TransactionIndex)
            .HasColumnName("transaction_index");
        builder.Property(x => x.EventIndex)
            .HasColumnName("event_index");
        builder.Property(x => x.ModuleReference)
            .HasColumnName("module_reference");
        builder.Property(x => x.ContractAddressIndex)
            .HasColumnName("contract_address_index");
        builder.Property(x => x.ContractAddressSubIndex)
            .HasColumnName("contract_address_sub_index");       
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}