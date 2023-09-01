namespace Application.Aggregates.Contract.Jobs;

public interface IContractJobFinder
{
    IEnumerable<IContractJob> GetJobs();
}