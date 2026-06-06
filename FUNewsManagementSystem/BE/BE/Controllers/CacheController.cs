using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using BE.Services;
using BE.Models;
using Microsoft.EntityFrameworkCore;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly INewArticleServices _newsArticleService;
        private readonly ICategoryServices _categoryService;
        private readonly ITagServices _tagService;
        private readonly ILogger<CacheController> _logger;

        // Cache keys
        private const string CACHE_KEY_NEWS = "cached_active_news";
        private const string CACHE_KEY_CATEGORIES = "cached_categories";
        private const string CACHE_KEY_TAGS = "cached_tags";
        private const string CACHE_KEY_LAST_UPDATE = "cache_last_update";

        // Cache duration
        private readonly TimeSpan NEWS_CACHE_DURATION = TimeSpan.FromMinutes(10);
        private readonly TimeSpan CATEGORY_CACHE_DURATION = TimeSpan.FromHours(1);
        private readonly TimeSpan TAG_CACHE_DURATION = TimeSpan.FromHours(1);

        public CacheController(
            IMemoryCache cache,
            INewArticleServices newsArticleService,
            ICategoryServices categoryService,
            ITagServices tagService,
            ILogger<CacheController> logger)
        {
            _cache = cache;
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _tagService = tagService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Cache/news - Lấy tin tức active từ cache hoặc database
        /// </summary>
        [HttpGet("news")]
        public async Task<IActionResult> GetCachedNews()
        {
            try
            {
                // Kiểm tra cache trước
                if (_cache.TryGetValue(CACHE_KEY_NEWS, out List<NewsArticle> cachedNews))
                {
                    _logger.LogInformation("Serving news from cache");
                    return Ok(new
                    {
                        status = StatusCodes.Status200OK,
                        message = "Success",
                        source = "cache",
                        lastUpdate = _cache.Get<DateTime?>(CACHE_KEY_LAST_UPDATE),
                        value = cachedNews
                    });
                }

                // Nếu không có cache, lấy từ database
                _logger.LogInformation("Cache miss - fetching news from database");
                var news = await _newsArticleService.GetAllNewsArticles()
                    .Where(n => n.NewsStatus == true)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(100)
                    .ToListAsync();

                // Lưu vào cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(NEWS_CACHE_DURATION)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(CACHE_KEY_NEWS, news, cacheOptions);
                _cache.Set(CACHE_KEY_LAST_UPDATE, DateTime.UtcNow, TimeSpan.FromHours(24));

                _logger.LogInformation("Cached {Count} news articles", news.Count);

                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    source = "database",
                    lastUpdate = DateTime.UtcNow,
                    value = news
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached news");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: api/Cache/categories - Lấy categories từ cache hoặc database
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCachedCategories()
        {
            try
            {
                // Kiểm tra cache
                if (_cache.TryGetValue(CACHE_KEY_CATEGORIES, out List<Category> cachedCategories))
                {
                    _logger.LogInformation("Serving categories from cache");
                    return Ok(new
                    {
                        status = StatusCodes.Status200OK,
                        message = "Success",
                        source = "cache",
                        value = cachedCategories
                    });
                }

                // Lấy từ database
                _logger.LogInformation("Cache miss - fetching categories from database");
                var categories = await _categoryService.GetAllCategories()
                    .Where(c => c.IsActive == true)
                    .ToListAsync();

                // Lưu vào cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CATEGORY_CACHE_DURATION)
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(CACHE_KEY_CATEGORIES, categories, cacheOptions);

                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    source = "database",
                    value = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached categories");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: api/Cache/tags - Lấy tags từ cache hoặc database
        /// </summary>
        [HttpGet("tags")]
        public async Task<IActionResult> GetCachedTags()
        {
            try
            {
                // Kiểm tra cache
                if (_cache.TryGetValue(CACHE_KEY_TAGS, out List<Tag> cachedTags))
                {
                    _logger.LogInformation("Serving tags from cache");
                    return Ok(new
                    {
                        status = StatusCodes.Status200OK,
                        message = "Success",
                        source = "cache",
                        value = cachedTags
                    });
                }

                // Lấy từ database
                _logger.LogInformation("Cache miss - fetching tags from database");
                var tags = await _tagService.GetAllTags().ToListAsync();

                // Lưu vào cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TAG_CACHE_DURATION)
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(CACHE_KEY_TAGS, tags, cacheOptions);

                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    source = "database",
                    value = tags
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached tags");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: api/Cache/status - Kiểm tra trạng thái cache
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetCacheStatus()
        {
            try
            {
                var hasNews = _cache.TryGetValue(CACHE_KEY_NEWS, out List<NewsArticle> newsCache);
                var hasCategories = _cache.TryGetValue(CACHE_KEY_CATEGORIES, out List<Category> categoryCache);
                var hasTags = _cache.TryGetValue(CACHE_KEY_TAGS, out List<Tag> tagCache);
                var lastUpdate = _cache.Get<DateTime?>(CACHE_KEY_LAST_UPDATE);

                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    data = new
                    {
                        cacheStatus = new
                        {
                            news = new
                            {
                                isCached = hasNews,
                                count = hasNews ? newsCache.Count : 0
                            },
                            categories = new
                            {
                                isCached = hasCategories,
                                count = hasCategories ? categoryCache.Count : 0
                            },
                            tags = new
                            {
                                isCached = hasTags,
                                count = hasTags ? tagCache.Count : 0
                            }
                        },
                        lastUpdate = lastUpdate,
                        isHealthy = hasNews && hasCategories && hasTags
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache status");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// POST: api/Cache/refresh - Refresh tất cả cache (xóa và load lại)
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshAllCache()
        {
            try
            {
                _logger.LogInformation("Manual cache refresh triggered");

                // Xóa cache cũ
                _cache.Remove(CACHE_KEY_NEWS);
                _cache.Remove(CACHE_KEY_CATEGORIES);
                _cache.Remove(CACHE_KEY_TAGS);

                // Load lại dữ liệu
                var news = await _newsArticleService.GetAllNewsArticles()
                    .Where(n => n.NewsStatus == true)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(100)
                    .ToListAsync();

                var categories = await _categoryService.GetAllCategories()
                    .Where(c => c.IsActive == true)
                    .ToListAsync();

                var tags = await _tagService.GetAllTags().ToListAsync();

                // Cache lại
                _cache.Set(CACHE_KEY_NEWS, news, NEWS_CACHE_DURATION);
                _cache.Set(CACHE_KEY_CATEGORIES, categories, CATEGORY_CACHE_DURATION);
                _cache.Set(CACHE_KEY_TAGS, tags, TAG_CACHE_DURATION);
                _cache.Set(CACHE_KEY_LAST_UPDATE, DateTime.UtcNow, TimeSpan.FromHours(24));

                _logger.LogInformation("Cache refreshed: {NewsCount} news, {CatCount} categories, {TagCount} tags",
                    news.Count, categories.Count, tags.Count);

                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    detail = "Cache refreshed successfully",
                    data = new
                    {
                        newsCount = news.Count,
                        categoriesCount = categories.Count,
                        tagsCount = tags.Count,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cache");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "Failed to refresh cache",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// POST: api/Cache/clear - Xóa tất cả cache
        /// </summary>
        [HttpPost("clear")]
        public IActionResult ClearCache()
        {
            try
            {
                _cache.Remove(CACHE_KEY_NEWS);
                _cache.Remove(CACHE_KEY_CATEGORIES);
                _cache.Remove(CACHE_KEY_TAGS);
                _cache.Remove(CACHE_KEY_LAST_UPDATE);

                _logger.LogInformation("Cache cleared successfully");

                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    detail = "Cache cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "Failed to clear cache",
                    detail = ex.Message
                });
            }
        }
    }
}