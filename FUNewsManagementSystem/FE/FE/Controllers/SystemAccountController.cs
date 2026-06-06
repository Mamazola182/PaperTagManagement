using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FE.Controllers
{
    public class SystemAccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "https://localhost:7135/api";

        public SystemAccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: View trang quản lý
        public IActionResult Index()
        {
            return View();
        }

        // GET: Lấy danh sách accounts với OData
        [HttpGet]
        public async Task<IActionResult> GetAccounts(
            int skip = 0,
            int top = 10,
            string? filter = null,
            string? search = null,
            int? roleFilter = null)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Build OData query
                var queryParams = new List<string>
                {
                    "$count=true",
                    $"$skip={skip}",
                    $"$top={top}",
                    "$expand=NewsArticles"
                };

                // Build filter
                var filters = new List<string>();

                if (!string.IsNullOrEmpty(search))
                {
                    var escapedTerm = search.Replace("'", "''").ToLower();
                    filters.Add($"(contains(tolower(AccountName), '{escapedTerm}') or contains(tolower(AccountEmail), '{escapedTerm}'))");
                }

                if (roleFilter.HasValue)
                {
                    filters.Add($"AccountRole eq {roleFilter.Value}");
                }

                if (filters.Count > 0)
                {
                    queryParams.Add($"$filter={string.Join(" and ", filters)}");
                }

                var queryString = string.Join("&", queryParams);
                var response = await client.GetAsync($"{_apiBaseUrl}/SystemAccount?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Json(new { success = false, message = $"Error: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Lấy chi tiết account
        [HttpGet]
        public async Task<IActionResult> GetAccountDetail(int id)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{_apiBaseUrl}/SystemAccount/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Json(new { success = false, message = $"Error: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Tạo account mới
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] JsonElement accountData)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = new StringContent(
                    accountData.GetRawText(),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}/SystemAccount", jsonContent);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // PUT: Cập nhật account
        [HttpPut]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] JsonElement accountData)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = new StringContent(
                    accountData.GetRawText(),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PutAsync($"{_apiBaseUrl}/SystemAccount/{id}", jsonContent);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // PUT: Đổi mật khẩu
        [HttpPut]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] JsonElement passwordData)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = new StringContent(
                    passwordData.GetRawText(),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PutAsync($"{_apiBaseUrl}/SystemAccount/ChangePassword/{id}", jsonContent);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // DELETE: Xóa account
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/SystemAccount/{id}");

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
