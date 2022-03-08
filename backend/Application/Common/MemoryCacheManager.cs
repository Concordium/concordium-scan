using System.Threading.Tasks;

namespace Application.Common;

public class MemoryCacheManager
{
    private readonly List<ICommittable> _items = new();

    public IMemoryCachedValue<T> CreateCachedValue<T>() where T : struct
    {
        var item = new MemoryCachedValue<T>();
        _items.Add(item);
        return item;
    }
    
    public void CommitEnqueuedUpdates()
    {
        foreach (var item in _items)
            item.Commit();
    }

    private class MemoryCachedValue<T> : ICommittable, IMemoryCachedValue<T> where T : struct
    {
        private T? _committedValue;
        private T? _enqueuedUpdatedValue;
        private bool _isInitialized = false;

        public T? GetCommittedValue()
        {
            return _committedValue;
        }

        public void EnqueueUpdate(T updatedValue)
        {
            _enqueuedUpdatedValue = updatedValue;
        }

        public async Task EnsureInitialized(Func<Task<T?>> initializeFunc)
        {
            if (!_isInitialized)
                _committedValue = await initializeFunc();
            _isInitialized = true;
        }

        public void Commit()
        {
            if (_enqueuedUpdatedValue.HasValue)
            {
                _committedValue = _enqueuedUpdatedValue;
                _enqueuedUpdatedValue = null;
            }
        }
    }

    private interface ICommittable
    {
        void Commit();
    }
}

public interface IMemoryCachedValue<T> where T : struct
{
    T? GetCommittedValue();
    void EnqueueUpdate(T updatedValue);
    Task EnsureInitialized(Func<Task<T?>> initializeFunc);
}
