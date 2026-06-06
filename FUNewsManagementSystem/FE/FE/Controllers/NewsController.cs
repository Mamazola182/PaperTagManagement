using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FE.Controllers
{
    public class NewsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string API_BASE = "https://localhost:7135";

        public NewsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Helper method to get auth token from cookie
        private string GetAuthToken()
        {
            return  HttpContext.Session.GetString("AuthToken") ?? "";
        }

        // Helper method to create HttpClient with auth header
        private HttpClient CreateAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        // Main page
        public IActionResult Index()
        {
            return View();
        }

        // Get all news with OData query
        [HttpGet]
        public async Task<IActionResult> GetNews(
            string? search,
            bool? status,
            string? dateFrom,
            string? dateTo,
            int page = 1,
            int pageSize = 9)
        {
            try
            {
                var client = CreateAuthenticatedClient();

                // Build OData query
                var filters = new List<string>();

                if (!string.IsNullOrEmpty(search))
                {
                    var lowerSearch = search.ToLower();
                    filters.Add($"(contains(tolower(NewsTitle), '{lowerSearch}') or contains(tolower(CreatedBy/AccountName), '{lowerSearch}') or contains(tolower(Category/CategoryName), '{lowerSearch}'))");
                }

                if (status.HasValue)
                {
                    filters.Add($"NewsStatus eq {status.Value.ToString().ToLower()}");
                }

                if (!string.IsNullOrEmpty(dateFrom))
                {
                    filters.Add($"CreatedDate ge {dateFrom}T00:00:00Z");
                }

                if (!string.IsNullOrEmpty(dateTo))
                {
                    filters.Add($"CreatedDate le {dateTo}T23:59:59Z");
                }

                var filterQuery = filters.Count > 0 ? $"$filter={string.Join(" and ", filters)}" : "";
                var skip = (page - 1) * pageSize;

                var queryParams = new List<string>
                {
                    "$expand=Category,CreatedBy,Tags",
                    "$orderby=CreatedDate desc",
                    $"$top={pageSize}",
                    $"$skip={skip}",
                    "$count=true"
                };

                if (!string.IsNullOrEmpty(filterQuery))
                {
                    queryParams.Add(filterQuery);
                }

                var odataQuery = string.Join("&", queryParams);
                var url = $"{API_BASE}/api/NewsArticle?{odataQuery}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Get categories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"{API_BASE}/api/Category");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Get tags
        [HttpGet]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"{API_BASE}/api/Tag");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Get news by ID
        [HttpGet]
        public async Task<IActionResult> GetNewsById(string id)
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"{API_BASE}/api/NewsArticle/{id}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Get category by ID
        [HttpGet]
        public async Task<IActionResult> GetCategoryById(string id)
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"{API_BASE}/api/Category/{id}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Get account by ID
        [HttpGet]
        public async Task<IActionResult> GetAccountById(string id)
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"{API_BASE}/api/SystemAccount/{id}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Create news
        [HttpPost]
        public async Task<IActionResult> CreateNews([FromBody] JsonElement newsData)
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var jsonContent = new StringContent(
                    newsData.GetRawText(),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{API_BASE}/api/NewsArticle", jsonContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Update news
        [HttpPut]
        public async Task<IActionResult> UpdateNews(string id, [FromBody] JsonElement newsData)
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var jsonContent = new StringContent(
                    newsData.GetRawText(),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PutAsync($"{API_BASE}/api/NewsArticle/{id}", jsonContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Delete news
        [HttpDelete]
        public async Task<IActionResult> DeleteNews(string id)
        {
            try
            {
                var client = CreateAuthenticatedClient();
                var response = await client.DeleteAsync($"{API_BASE}/api/NewsArticle/{id}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
