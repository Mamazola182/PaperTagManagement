using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using BE.Services;
using BE.Models;
namespace CoreAPI.Services
{
    public class CacheStarupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheStarupService> _logger;

        public CacheStarupService(
            IServiceProvider serviceProvider,
            ILogger<CacheStarupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Starting cache initialization...");

            try
            {
                // Tạo scope để lấy scoped services
                using var scope = _serviceProvider.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                var newsService = scope.ServiceProvider.GetRequiredService<INewArticleServices>();
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryServices>();
                var tagService = scope.ServiceProvider.GetRequiredService<ITagServices>();

                // Cache keys
                const string CACHE_KEY_NEWS = "cached_active_news";
                const string CACHE_KEY_CATEGORIES = "cached_categories";
                const string CACHE_KEY_TAGS = "cached_tags";
                const string CACHE_KEY_LAST_UPDATE = "cache_last_update";

                // 1. Cache Active News
                _logger.LogInformation("📰 Loading active news...");
                var news = await newsService.GetAllNewsArticles()
                    .Where(n => n.NewsStatus == true)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                var newsCacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetPriority(CacheItemPriority.High);

                cache.Set(CACHE_KEY_NEWS, news, newsCacheOptions);
                _logger.LogInformation("✅ Cached {Count} active news articles", news.Count);

                // 2. Cache Categories
                _logger.LogInformation("📁 Loading categories...");
                var categories = await categoryService.GetAllCategories()
                    .Where(c => c.IsActive == true)
                    .ToListAsync(cancellationToken);

                var categoryCacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                    .SetPriority(CacheItemPriority.Normal);

                cache.Set(CACHE_KEY_CATEGORIES, categories, categoryCacheOptions);
                _logger.LogInformation("✅ Cached {Count} categories", categories.Count);

                // 3. Cache Tags
                _logger.LogInformation("🏷️ Loading tags...");
                var tags = await tagService.GetAllTags().ToListAsync(cancellationToken);

                var tagCacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                    .SetPriority(CacheItemPriority.Normal);

                cache.Set(CACHE_KEY_TAGS, tags, tagCacheOptions);
                _logger.LogInformation("✅ Cached {Count} tags", tags.Count);

                // 4. Set last update time
                cache.Set(CACHE_KEY_LAST_UPDATE, DateTime.UtcNow, TimeSpan.FromHours(24));

                _logger.LogInformation("🎉 Cache initialization completed successfully!");
                _logger.LogInformation("📊 Summary: {NewsCount} news, {CatCount} categories, {TagCount} tags",
                    news.Count, categories.Count, tags.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize cache on startup");
                // Không throw exception để app vẫn chạy được
            }

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache startup service is stopping.");
            return Task.CompletedTask;
        }
    }
}
