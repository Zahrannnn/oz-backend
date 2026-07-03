using System.Collections.Concurrent;

namespace Oz.Api.Middleware;

public class RateLimitEntry
{
    private readonly int _windowSeconds;
    private readonly Queue<DateTime> _timestamps = new();
    private readonly object _lock = new();

    public RateLimitEntry(int windowSeconds) => _windowSeconds = windowSeconds;

    public bool TryConsume(int limit, out int retryAfter)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            while (_timestamps.Count > 0 && (now - _timestamps.Peek()).TotalSeconds > _windowSeconds)
                _timestamps.Dequeue();

            if (_timestamps.Count >= limit)
            {
                retryAfter = _windowSeconds - (int)(now - _timestamps.Peek()).TotalSeconds;
                return false;
            }

            _timestamps.Enqueue(now);
            retryAfter = 0;
            return true;
        }
    }
}
