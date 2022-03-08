using Application.Common;

namespace Tests.TestUtilities;

public class MemoryCachedValueStub<T> : IMemoryCachedValue<T> where T : struct
{
    private T? _committedValue;
    private T? _queuedUpdate;
    private bool _isInitialized = false;

    public T? GetCommittedValue()
    {
        return _committedValue;
    }

    public void EnqueueUpdate(T updatedValue)
    {
        _queuedUpdate = updatedValue;
    }

    public async Task EnsureInitialized(Func<Task<T?>> initializeFunc)
    {
        if (!_isInitialized)
        {
            _committedValue = await initializeFunc();
            _isInitialized = true;
        }
    }

    public void Initialize(T value)
    {
        _committedValue = value;
        _isInitialized = true;
    }

    public void Reset()
    {
        _committedValue = null;
        _isInitialized = false;
        _queuedUpdate = null;
    }

    public T? GetEnqueuedUpdate()
    {
        return _queuedUpdate;
    }
}