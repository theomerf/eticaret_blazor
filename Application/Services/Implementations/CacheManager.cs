using Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Application.Services.Implementations
{
    public class CacheManager : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly HashSet<string> _keys = new();
        private readonly object _keysLock = new();

        public CacheManager(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            _cache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null,
            CancellationToken ct = default)
        {
            if (_cache.TryGetValue(key, out T? cached) && cached is not null)
                return cached;

            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(ct);

            try
            {
                if (_cache.TryGetValue(key, out cached) && cached is not null)
                    return cached;

                var value = await factory(ct);

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5),
                    SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(2)
                };

                _cache.Set(key, value, options);
                TrackKey(key);

                return value;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public Task SetAsync<T>(string key, T value,
            TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null,
            CancellationToken ct = default)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5),
                SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(2)
            };

            _cache.Set(key, value, options);
            TrackKey(key);

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _cache.Remove(key);
            lock (_keysLock) _keys.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            List<string> keysToRemove;

            lock (_keysLock)
            {
                keysToRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
                foreach (var key in keysToRemove)
                    _keys.Remove(key);
            }

            foreach (var key in keysToRemove)
                _cache.Remove(key);

            return Task.CompletedTask;
        }

        private void TrackKey(string key)
        {
            lock (_keysLock) _keys.Add(key);
        }
    }
}
