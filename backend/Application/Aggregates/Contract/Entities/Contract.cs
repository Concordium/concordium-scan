using Application.Aggregates.Contract.Exceptions;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Initial event stored when a smart contract is created
/// These are non mutable values through the lifetime of the smart contract.
/// </summary>
public sealed class Contract
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; }
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public AccountAddress Creator { get; init; }
    public ImportSource Source { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public ICollection<ContractEvent> ContractEvents { get; set; }
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private Contract()
#pragma warning restore CS8618
    {}

    internal Contract(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        ContractAddress contractAddress,
        AccountAddress creator,
        ImportSource source)
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        EventIndex = eventIndex;
        Creator = creator;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Source = source;
    }
    
    [ExtendObjectType(typeof(Query))]
    public class ContractQuery
    {
        [UsePaging]
        public IQueryable<Contract> GetContracts(
            GraphQlDbContext context)
        {
            return context.Contract
                .AsNoTracking()
                .Include(s => s.ContractEvents);
        }
    }

    /// <summary>
    /// Adds additional field to the returned GraphQL type <see cref="Contract"/>
    /// </summary>
    [ExtendObjectType(typeof(Contract))]
    public sealed class ContractExtensions
    {
        public ContractAddress GetContractAddress([Parent] Contract contract) => 
            new(contract.ContractAddressIndex, contract.ContractAddressSubIndex);

        /// <summary>
        /// Returns aggregated amount from events on contract.
        /// </summary>
        public double GetAmount([Parent] Contract contract)
        {
            if (contract.ContractEvents is null)
            {
                return 0;
            }

            var amount = 0L;
            foreach (var contractEvent in contract.ContractEvents)
            {
                switch (contractEvent.Event)
                {
                    case ContractInitialized contractInitialized:
                        amount += (long)contractInitialized.Amount;
                        break;
                    case ContractUpdated contractUpdated:
                        amount += (long)contractUpdated.Amount;
                        break;
                    case Transferred transferred:
                        if (transferred.From is not ContractAddress contractAddress)
                        {
                            throw new ContractQueryException(
                                $"Got transfer with txHash, {contractEvent.TransactionHash}, with event on contract <{contract.ContractAddressIndex},{contract.ContractAddressSubIndex}> with FROM which wasn't a contract address.");
                        }
                        if (contractAddress.Index != contract.ContractAddressIndex ||
                            contractAddress.SubIndex != contract.ContractAddressSubIndex)
                        {
                            throw new ContractQueryException(
                                $"Got transfer with txHash, {contractEvent.TransactionHash}, with event on contract <{contract.ContractAddressIndex},{contract.ContractAddressSubIndex}> with FROM which wasn't same contract address but instead <{contractAddress.Index},{contractAddress.SubIndex}>");
                        }
                        amount -= (long)transferred.Amount;
                        break;
                }
            }

            return amount;
        }
    }
}