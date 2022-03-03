using System.Collections;

namespace Application.Common;

public class RangeOfLong : IEnumerable<long>
{
    private readonly long _start;
    private readonly long _end;

    public RangeOfLong(long start, long end)
    {
        _start = start;
        _end = end;
    }

    public IEnumerator<long> GetEnumerator()
    {
        return new RangeEnumerator(_start, _end);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class RangeEnumerator : IEnumerator<long>
    {
        private readonly long _start;
        private readonly long _end;
        private long _current;
        private bool _started = false;
        private bool _ended = false;

        public RangeEnumerator(long start, long end)
        {
            _start = start;
            _end = end;
        }

        public bool MoveNext()
        {
            if (!_started)
            {
                _current = _start;
                _started = true;
                return true;
            }

            if (_current < _end)
            {
                _current += 1;
                return true;
            }

            _current = _end;
            return false;
        }

        public void Reset()
        {
            _started = false;
            _ended = false;
        }

        object IEnumerator.Current => Current;

        public long Current
        {
            get
            {
                if (!_started) throw new InvalidOperationException("Enumerator not started");
                if (_ended) throw new InvalidOperationException("Enumerator ended");
                return _current;
            }
        }

        public void Dispose()
        {
        }
    }
}