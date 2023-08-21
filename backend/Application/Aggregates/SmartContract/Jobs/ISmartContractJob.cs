using System.Threading;
using System.Threading.Tasks;

namespace Application.Aggregates.SmartContract.Jobs;

/// <summary>
/// Interfaces which should be used for all jobs relevant for
/// Smart Contracts. 
/// </summary>
public interface ISmartContractJob
{
    Task StartImport(CancellationToken token);
    string GetUniqueIdentifier();
}