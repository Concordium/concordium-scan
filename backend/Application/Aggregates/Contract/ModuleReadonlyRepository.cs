using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract;

/// <summary>
/// Repository which contains data access to <see cref="Application.Aggregates.Contract.Entities.ModuleReferenceEvent"/>
/// related entities.
///
/// Entities returned are always readonly hence any changes to them will not be persisted. Any entity mutation should
/// be done through aggregate repository <see cref="IContractRepository"/>.
/// </summary>
public interface IModuleReadonlyRepository : IAsyncDisposable
{
    /// <summary>
    /// Get <see cref="ModuleReferenceEvent"/> from module reference.
    ///
    /// Entity is read only and changes on entity will not be persisted.
    /// </summary>
    /// <exception cref="Exception">
    /// Throws exception if module doesn't exists.
    /// </exception>
    Task<ModuleReferenceEvent> GetModuleReferenceEventAsync(string moduleReference);
    /// <summary>
    /// Get references module for the given <see cref="contractAddress"/> at
    /// <see cref="blockHeight"/>, <see cref="transactionIndex"/>, <see cref="eventIndex"/>.
    ///
    /// Entity is read only and changes on entity will not be persisted.
    /// </summary>
    Task<ModuleReferenceEvent> GetModuleReferenceEventAtAsync(ContractAddress contractAddress, ulong blockHeight, ulong transactionIndex, uint eventIndex);
}


internal sealed class ModuleReadonlyRepository : IModuleReadonlyRepository
{
    private readonly GraphQlDbContext _context;

    public ModuleReadonlyRepository(GraphQlDbContext context)
    {
        _context = context;
    }
    
    /// <inheritdoc/>
    public Task<ModuleReferenceEvent> GetModuleReferenceEventAsync(string moduleReference)
    {
        return _context.ModuleReferenceEvents
            .AsNoTracking()
            .FirstAsync(m => m.ModuleReference == moduleReference);
    }

    /// <summary>
    /// Starts by looking after <see cref="ModuleReferenceContractLinkEvent"/> with <see cref="ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added"/>
    /// for the given <see cref="contractAddress"/> in the change provider of Entity Framework. These are the entity which has been added in the current transaction
    /// but are not yet committed to the database.
    ///
    /// If none is present the database is queried.
    ///
    /// The query use find the first event which has a <see cref="blockHeight"/> equal to or less than the input.
    /// If the event returned equals in block height another query is invoked which filters for <see cref="transactionIndex"/>
    /// equal or below.
    /// If the event returned equals in transaction index a last query is invoked which filter for <see cref="eventIndex"/>.
    ///
    /// In most cases there will be a difference in <see cref="blockHeight"/> and the main execution path will only introduce one query.
    /// </summary>
    public async Task<ModuleReferenceEvent> GetModuleReferenceEventAtAsync(ContractAddress contractAddress, ulong blockHeight, ulong transactionIndex,
        uint eventIndex)
    {
        var link = _context.ChangeTracker
            .Entries<ModuleReferenceContractLinkEvent>()
            .Select(e => e.Entity)
            .Where(l => 
                l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                l.BlockHeight <= blockHeight &&
                l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .OrderByDescending(l => l.BlockHeight)
            .ThenByDescending(l => l.TransactionIndex)
            .ThenByDescending(l => l.EventIndex)
            .FirstOrDefault();

        // Since block height is the same we need to make a query which only finds transaction index below input.
        if (link != null && link.BlockHeight == blockHeight)
        {
            link = _context.ChangeTracker
                .Entries<ModuleReferenceContractLinkEvent>()
                .Select(e => e.Entity)
                .Where(l => 
                    l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                    l.BlockHeight <= blockHeight && l.TransactionIndex <= transactionIndex &&
                    l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                .OrderByDescending(l => l.BlockHeight)
                .ThenByDescending(l => l.TransactionIndex)
                .ThenByDescending(l => l.EventIndex)
                .FirstOrDefault();
            
            // Since transaction index is the same we need to make a query which only finds event index below input.
            if (link != null && link.TransactionIndex == transactionIndex)
            {
                link = _context.ChangeTracker
                    .Entries<ModuleReferenceContractLinkEvent>()
                    .Select(e => e.Entity)
                    .Where(l => 
                        l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                        l.BlockHeight <= blockHeight && l.TransactionIndex <= transactionIndex && l.EventIndex <= eventIndex &&
                        l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                    .OrderByDescending(l => l.BlockHeight)
                    .ThenByDescending(l => l.TransactionIndex)
                    .ThenByDescending(l => l.EventIndex)
                    .FirstOrDefault();
            }
        }

        if (link == null)
        {
            try
            {
                link = await _context.ModuleReferenceContractLinkEvents
                    .Where(l => 
                        l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                        l.BlockHeight <= blockHeight &&
                        l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                    .OrderByDescending(l => l.BlockHeight)
                    .ThenByDescending(l => l.TransactionIndex)
                    .ThenByDescending(l => l.EventIndex)
                    .FirstAsync();   
                
                // Since block height is the same we need to make a query which only finds transaction index below input.
                if (link.BlockHeight == blockHeight)
                {
                    link = await _context.ModuleReferenceContractLinkEvents
                        .Where(l => 
                            l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                            l.BlockHeight <= blockHeight && l.TransactionIndex <= transactionIndex &&
                            l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                        .OrderByDescending(l => l.BlockHeight)
                        .ThenByDescending(l => l.TransactionIndex)
                        .ThenByDescending(l => l.EventIndex)
                        .FirstAsync();

                    // Since transaction index is the same we need to make a query which only finds event index below input.
                    if (link.TransactionIndex == transactionIndex)
                    {
                        link = await _context.ModuleReferenceContractLinkEvents
                            .Where(l => 
                                l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                                l.BlockHeight <= blockHeight && l.TransactionIndex <= transactionIndex && l.EventIndex <= eventIndex &&
                                l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                            .OrderByDescending(l => l.BlockHeight)
                            .ThenByDescending(l => l.TransactionIndex)
                            .ThenByDescending(l => l.EventIndex)
                            .FirstAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        var module = await _context.ModuleReferenceEvents
            .FirstAsync(m => m.ModuleReference == link.ModuleReference);
        
        return module;
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
