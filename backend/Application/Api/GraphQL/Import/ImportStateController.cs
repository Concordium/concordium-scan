using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class ImportStateController
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;
    private ImportState? _lastCommittedState;
    private ImportState? _lastSavedState;

    public ImportStateController(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<ImportState> GetState()
    {
        var result = await GetStateIfExists();
        return result ?? throw new InvalidOperationException("No persisted state found in database!");
    }

    public async Task<ImportState?> GetStateIfExists()
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportStateController), nameof(GetStateIfExists));

        if (_lastCommittedState == null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var state = await dbContext.ImportState.SingleOrDefaultAsync();
            if (state == null) return null;
            _lastCommittedState = state;
        }

        var result = new ImportState();
        result.CopyValuesFrom(_lastCommittedState);
        return result;
    }

    public async Task SaveChanges(ImportState state)
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportStateController), nameof(SaveChanges));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        if (state.Id == 0 && _lastCommittedState == null) // Genesis state
        {
            dbContext.ImportState.Add(state);
            _lastSavedState = state;
        }
        else if (state.Id > 0 && _lastCommittedState != null)
        {
            _lastSavedState = _lastCommittedState;
            _lastCommittedState = null;
            
            dbContext.ImportState.Attach(_lastSavedState);
            _lastSavedState.CopyValuesFrom(state);
        }
        else
            throw new InvalidOperationException("Invalid internal state");

        await dbContext.SaveChangesAsync();
    }

    public void SavedChangesCommitted()
    {
        _lastCommittedState = _lastSavedState ?? throw new InvalidOperationException("Last saved state was null.");
        _lastSavedState = null;
    }
}