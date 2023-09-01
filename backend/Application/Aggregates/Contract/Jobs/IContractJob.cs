using System.Threading;
using System.Threading.Tasks;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// Interfaces which should be used for all jobs relevant for
/// Smart Contracts. 
/// </summary>
public interface IContractJob
{
    Task StartImport(CancellationToken token);
    string GetUniqueIdentifier();
}