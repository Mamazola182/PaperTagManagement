using Microsoft.Extensions.Caching.Memory;

namespace CoreAPI.Services
{
    public class TagCacheService
    {
        private readonly IMemoryCache _cache;
        private const string CacheKey = "LearningCache";

        public TagCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Dictionary<string, int> GetCache()
        {
            if (!_cache.TryGetValue(CacheKey, out Dictionary<string, int>? cacheData))
            {
                cacheData = new();
                _cache.Set(CacheKey, cacheData);
            }
            return cacheData!;
        }

        public void UpdateCache(IEnumerable<string> tags)
        {
            var cacheData = GetCache();
            foreach (var tag in tags)
            {
                if (cacheData.ContainsKey(tag))
                    cacheData[tag]++;
                else
                    cacheData[tag] = 1;
            }
            _cache.Set(CacheKey, cacheData);
        }

        public List<string> GetTopTags(int count = 10)
        {
            var cacheData = GetCache();
            return cacheData.OrderByDescending(kv => kv.Value)
                            .Take(count)
                            .Select(kv => kv.Key)
                            .ToList();
        }
    }
}
