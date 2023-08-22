namespace Application.Aggregates.SmartContract.Jobs;

public interface ISmartContractJobFinder
{
    IEnumerable<ISmartContractJob> GetJobs();
}