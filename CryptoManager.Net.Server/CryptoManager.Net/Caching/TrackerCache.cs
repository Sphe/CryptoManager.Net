using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CryptoManager.Net.Caching
{
    public interface ITrackerCache
    {
        T? TryGet<T>(string key);
        void Set<T>(string key, T data, TimeSpan expire);
    }

    internal class TrackerCache : ITrackerCache
    {
        private IMemoryCache _cache;

        public TrackerCache()
        {
            _cache = new MemoryCache(Options.Create(new MemoryCacheOptions
            {
                SizeLimit = 1024
            }));
        }


        public T? TryGet<T>(string key) => _cache.Get<T>(key);
        public void Set<T>(string key, T data, TimeSpan expire) => _cache.Set(key, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expire,
            Size = 1
        });
    }
}
