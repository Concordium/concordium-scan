using Application.Aggregates.SmartContract.Exceptions;
using Application.Aggregates.SmartContract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Initial event stored when a smart contract is created
/// These are non mutable values through the lifetime of the smart contract.
/// </summary>
public sealed class SmartContract
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
    public ICollection<SmartContractEvent> SmartContractEvents { get; set; }
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private SmartContract()
#pragma warning restore CS8618
    {}

    internal SmartContract(
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
    public class SmartContractQuery
    {
        [UsePaging]
        public IQueryable<SmartContract> GetSmartContracts(
            GraphQlDbContext context)
        {
            return context.SmartContract
                .AsNoTracking()
                .Include(s => s.SmartContractEvents);
        }
    }

    [ExtendObjectType(typeof(SmartContract))]
    public sealed class SmartContractExtensions
    {
        /// <summary>
        /// Adds additional field to the returned GraphQL type <see cref="SmartContract"/> which returns
        /// aggregated amount from events on contract.
        /// </summary>
        public double GetAmount([Parent] SmartContract smartContract)
        {
            if (smartContract.SmartContractEvents is null)
            {
                return 0;
            }

            var amount = 0L;
            foreach (var contractEvent in smartContract.SmartContractEvents)
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
                            throw new SmartContractQueryException(
                                $"Got transfer with txHash, {contractEvent.TransactionHash}, with event on contract <{smartContract.ContractAddressIndex},{smartContract.ContractAddressSubIndex}> with FROM which wasn't a contract address.");
                        }
                        if (contractAddress.Index != smartContract.ContractAddressIndex ||
                            contractAddress.SubIndex != smartContract.ContractAddressSubIndex)
                        {
                            throw new SmartContractQueryException(
                                $"Got transfer with txHash, {contractEvent.TransactionHash}, with event on contract <{smartContract.ContractAddressIndex},{smartContract.ContractAddressSubIndex}> with FROM which wasn't same contract address but instead <{contractAddress.Index},{contractAddress.SubIndex}>");
                        }
                        amount -= (long)transferred.Amount;
                        break;
                }
            }

            return amount;
        }
    }
}
