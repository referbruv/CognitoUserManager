using CognitoUserManager.Contracts.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CognitoUserManager.Core.Services
{
    public class CacheService : IPersistService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T Get<T>(string key)
        {
            T value;
            if (!_cache.TryGetValue<T>(key, out value))
            {
                return default;
            }
            else
            {
                return value;
            }
        }

        public void Set<T>(string key, T value)
        {
            _cache.Set<T>(key, value);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}
