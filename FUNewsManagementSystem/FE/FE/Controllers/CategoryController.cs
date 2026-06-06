using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FE.DTO;    
namespace FE.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public CategoryController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7135";
        }

        // GET: Category/Index - Hiển thị trang quản lý danh mục
        public IActionResult Index()
        {
            return View();
        }

        // GET: Category/GetCategories - Lấy danh sách danh mục với OData
        // Cách đơn giản nhất: Trả về raw JSON từ API
        [HttpGet]
        public async Task<IActionResult> GetCategories(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? statusFilter = null)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Build OData query
                var skip = (page - 1) * pageSize;
                var odataQuery = $"$count=true&$skip={skip}&$top={pageSize}";

                // Build filters
                var filters = new List<string>();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var escapedTerm = searchTerm.Replace("'", "''").ToLower();
                    filters.Add($"(contains(tolower(CategoryName), '{escapedTerm}') or contains(tolower(CategoryDesciption), '{escapedTerm}'))");
                }

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    filters.Add($"IsActive eq {statusFilter}");
                }

                if (filters.Any())
                {
                    odataQuery += $"&$filter={string.Join(" and ", filters)}";
                }

                var response = await client.GetAsync($"{_apiBaseUrl}/api/Category?{odataQuery}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Trả về raw JSON - giữ nguyên @odata.count
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, new { message = "Không thể tải danh sách danh mục" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // GET: Category/GetCategory/5 - Lấy chi tiết danh mục
        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{_apiBaseUrl}/api/Category/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }

                return StatusCode((int)response.StatusCode, new { message = "Không thể tải thông tin danh mục" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // GET: Category/GetCategoryWithNews/5 - Lấy danh mục kèm tin tức
        [HttpGet]
        public async Task<IActionResult> GetCategoryWithNews(int id)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Gọi song song 2 API
                var categoryTask = client.GetAsync($"{_apiBaseUrl}/api/Category/{id}");
                var newsTask = client.GetAsync($"{_apiBaseUrl}/api/Category/AllNews/{id}");

                await Task.WhenAll(categoryTask, newsTask);

                var categoryResponse = await categoryTask;
                var newsResponse = await newsTask;

                if (categoryResponse.IsSuccessStatusCode && newsResponse.IsSuccessStatusCode)
                {
                    var categoryContent = await categoryResponse.Content.ReadAsStringAsync();
                    var newsContent = await newsResponse.Content.ReadAsStringAsync();

                    var category = JsonSerializer.Deserialize<object>(categoryContent);
                    var news = JsonSerializer.Deserialize<object>(newsContent);

                    return Ok(new { category, news });
                }

                return StatusCode(500, new { message = "Không thể tải thông tin chi tiết" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // POST: Category/CreateCategory - Tạo danh mục mới
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryModel model)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { errors = ModelState });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = JsonSerializer.Serialize(model);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}/api/CreateCategory", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(new { message = "Tạo danh mục thành công", data = JsonSerializer.Deserialize<object>(content) });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { message = "Không thể tạo danh mục", error = errorContent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // PUT: Category/UpdateCategory/5 - Cập nhật danh mục
        [HttpPut]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryModel model)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { errors = ModelState });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                model.CategoryId = id;
                var jsonContent = JsonSerializer.Serialize(model);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{_apiBaseUrl}/api/Category/{id}", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Cập nhật danh mục thành công" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { message = "Không thể cập nhật danh mục", error = errorContent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // DELETE: Category/DeleteCategory/5 - Xóa danh mục
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/Category/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Xóa danh mục thành công" });
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return Conflict(new { message = "Không thể xóa danh mục này vì đang có bài viết liên kết" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { message = "Không thể xóa danh mục", error = errorContent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }
    }

    // Models
    public class CategoryModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryDesciption { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ODataResponse
    {
        public List<object> Value { get; set; } = new();
        public int Count { get; set; }
    }
}

