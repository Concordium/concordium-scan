using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Transactions;
using HotChocolate;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Contains aggregated local for <see cref="Contract"/>.
/// </summary>
public sealed class ContractSnapshot
{
    public ulong BlockHeight { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public string ContractName { get; init; }
    public string ModuleReference { get; init; }
    public double Amount { get; init; }
    
    [GraphQLIgnore]
    public ImportSource Source { get; init; }
    [GraphQLIgnore]
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ContractSnapshot()
    {
    }

    internal ContractSnapshot(
        ulong blockHeight,
        ContractAddress contractAddress,
        string contractName,
        string moduleReference,
        double amount,
        ImportSource importSource
        )
    {
        BlockHeight = blockHeight;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        ContractName = contractName;
        ModuleReference = moduleReference;
        Amount = amount;
        Source = importSource;
    }

    internal static async Task ImportContractSnapshot(
        IContractRepositoryFactory repositoryFactory,
        IContractRepository repository, 
        ImportSource source
        )
    {
        await using var moduleReadonlyRepository = await repositoryFactory.CreateModuleReadonlyRepository();
            
        var contractEventsAdded = repository
            .GetContractEventsAddedInTransaction()
            .ToList();
        
        var addressesInitialized = contractEventsAdded
            .Where(ce => ce.Event is ContractInitialized)
            .Select(ce => (ce.ContractAddressIndex, ce.ContractAddressSubIndex))
            .ToHashSet();

        var addressesUpdated = contractEventsAdded
            .Where(ce => !addressesInitialized.Contains((ce.ContractAddressIndex, ce.ContractAddressSubIndex)))
            .Select(ce => (ce.ContractAddressIndex, ce.ContractAddressSubIndex))
            .ToHashSet();

        await AddInitialContractSnapshots(addressesInitialized, contractEventsAdded, repository, moduleReadonlyRepository, source);

        await AddUpdatedSnapshots(addressesUpdated, contractEventsAdded, repository, moduleReadonlyRepository, source);
    }

    private static async Task AddUpdatedSnapshots(
        IEnumerable<(ulong ContractAddressIndex, ulong ContractAddressSubIndex)> addressesUpdated,
        IList<ContractEvent> contractEventsAdded,
        IContractRepository repository, 
        IModuleReadonlyRepository moduleReadonlyRepository,
        ImportSource source
        )
    {
        foreach (var contractAddress in addressesUpdated)
        {
            var address = new ContractAddress(contractAddress.ContractAddressIndex, contractAddress.ContractAddressSubIndex);
            var contractEvents = contractEventsAdded
                .Where(ce => ce.ContractAddressIndex == contractAddress.ContractAddressIndex && ce.ContractAddressSubIndex == contractAddress.ContractAddressSubIndex)
                .OrderBy(ce => ce.BlockHeight)
                .ThenBy(ce => ce.TransactionIndex)
                .ThenBy(ce => ce.EventIndex)
                .ToList();

            var latestContractSnapshot = await repository.GetReadonlyLatestContractSnapshot(address);
            var blockHeight = contractEvents.Last().BlockHeight;
            var moduleReferenceEvent = GetLatestModuleReferenceContractLinkEvent(address, moduleReadonlyRepository)!;
            var amount = GetAmount(contractEvents, address, latestContractSnapshot.Amount);

            var contractSnapshot = new ContractSnapshot(
                blockHeight,
                address,
                latestContractSnapshot.ContractName,
                moduleReferenceEvent.ModuleReference,
                amount,
                source
            );

            await repository.AddAsync(contractSnapshot);
        }
    }

    private static async Task AddInitialContractSnapshots(
        IEnumerable<(ulong ContractAddressIndex, ulong ContractAddressSubIndex)> addressesInitialized,
        IList<ContractEvent> contractEventsAdded,
        IContractRepository repository, 
        IModuleReadonlyRepository moduleReadonlyRepository,
        ImportSource source
        )
    {
        foreach (var contractAddress in addressesInitialized)
        {
            var address = new ContractAddress(contractAddress.ContractAddressIndex, contractAddress.ContractAddressSubIndex);
            var contractEvents = contractEventsAdded
                .Where(ce => ce.ContractAddressIndex == contractAddress.ContractAddressIndex && ce.ContractAddressSubIndex == contractAddress.ContractAddressSubIndex)
                .OrderBy(ce => ce.BlockHeight)
                .ThenBy(ce => ce.TransactionIndex)
                .ThenBy(ce => ce.EventIndex)
                .ToList();

            var contractInitializationEvent = (contractEvents.First().Event as ContractInitialized)!;
            var amount = GetAmount(contractEvents, address, 0);
            
            var blockHeight = contractEvents.Last().BlockHeight;
            
            var moduleReferenceEvent = GetLatestModuleReferenceContractLinkEvent(address, moduleReadonlyRepository)!;

            var contractSnapshot = new ContractSnapshot(
                blockHeight,
                address,
                contractInitializationEvent.GetName(),
                moduleReferenceEvent.ModuleReference,
                amount,
                source
            );

            await repository.AddAsync(contractSnapshot);
        }
    }

    private static ModuleReferenceContractLinkEvent? GetLatestModuleReferenceContractLinkEvent(ContractAddress contractAddress, IModuleReadonlyRepository moduleReadonlyRepository) =>
        moduleReadonlyRepository.GetModuleReferenceContractLinkEventInTransaction(contractAddress)
            .OrderByDescending(m => m.BlockHeight)
            .ThenByDescending(m => m.TransactionIndex)
            .ThenByDescending(m => m.EventIndex)
            .FirstOrDefault();

    internal static double GetAmount(
        IEnumerable<ContractEvent> contractEvents,
        ContractAddress contractAddress,
        double initialAmount)
    {
        var amount = initialAmount;
        foreach (var contractEvent in contractEvents)
        {
            switch (contractEvent.Event)
            {
                case ContractInitialized contractInitialized:
                    amount += (long)contractInitialized.Amount;
                    break;
                case ContractUpdated contractUpdated:
                    amount += (long)contractUpdated.Amount;
                    break;
                case ContractCall contractCall:
                {
                    if (contractCall.ContractUpdated.Instigator is ContractAddress instigator &&
                        instigator.Index == contractAddress.Index &&
                        instigator.SubIndex == contractAddress.SubIndex)
                    {
                        amount -= (long)contractCall.ContractUpdated.Amount;
                    }                        
                    break;
                }
                case Transferred transferred:
                    if (transferred.From is ContractAddress contractAddressFrom &&
                        contractAddressFrom.Index == contractAddress.Index &&
                        contractAddressFrom.SubIndex == contractAddress.SubIndex)
                    {
                        amount -= (long)transferred.Amount;
                    }
                    break;
            }
        }

        return amount;
    }
}
