using FE.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FE.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "https://localhost:7135";
        private readonly IConfiguration _configuration;
        public AuthController(ILogger<AuthController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email và mật khẩu không được để trống!" });
                }

                // Create HttpClient
                var client = _httpClientFactory.CreateClient();
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/auth/login";

                // Prepare request body
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                // Call API
                var response = await client.PostAsync(apiUrl, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Return raw JSON to JavaScript
                if (response.IsSuccessStatusCode)
                {
                    // Parse JSON to extract tokens and save to session
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        var root = doc.RootElement;

                        // Try to get tokens from different possible structures
                        string? token = null;
                        string? refreshToken = null;

                        // Structure 1: { "data": { "token": "...", "refreshToken": "..." } }
                        if (root.TryGetProperty("data", out JsonElement dataElement))
                        {
                            if (dataElement.TryGetProperty("token", out JsonElement tokenElement))
                            {
                                token = tokenElement.GetString();
                            }
                            if (dataElement.TryGetProperty("refreshToken", out JsonElement refreshTokenElement))
                            {
                                refreshToken = refreshTokenElement.GetString();
                            }
                        }
                        // Structure 2: { "token": "...", "refreshToken": "..." }
                        else
                        {
                            if (root.TryGetProperty("token", out JsonElement directToken))
                            {
                                token = directToken.GetString();
                            }
                            if (root.TryGetProperty("refreshToken", out JsonElement directRefreshToken))
                            {
                                refreshToken = directRefreshToken.GetString();
                            }
                        }

                        // Save tokens to session
                        if (!string.IsNullOrEmpty(token))
                        {
                            HttpContext.Session.SetString("AuthToken", token);
                        }
                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            HttpContext.Session.SetString("RefreshToken", refreshToken);
                        }

                        // Optionally save user info
                        if (root.TryGetProperty("data", out JsonElement userData))
                        {
                            if (userData.TryGetProperty("user", out JsonElement userElement) ||
                                userData.TryGetProperty("userInfo", out userElement))
                            {
                                HttpContext.Session.SetString("UserInfo", userElement.GetRawText());
                            }
                        }
                    }

                    return Content(responseContent, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, responseContent);
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { message = "Không thể kết nối đến API server!", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi!", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = HttpContext.Session.GetString("RefreshToken");

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { message = "Không tìm thấy refresh token!" });
                }

                // Create HttpClient
                var client = _httpClientFactory.CreateClient();
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/auth/refresh-token";

                // Prepare request body
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new { refreshToken }),
                    Encoding.UTF8,
                    "application/json"
                );

                // Call API
                var response = await client.PostAsync(apiUrl, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse and update tokens in session
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        var root = doc.RootElement;

                        string? newToken = null;
                        string? newRefreshToken = null;

                        if (root.TryGetProperty("data", out JsonElement dataElement))
                        {
                            if (dataElement.TryGetProperty("token", out JsonElement tokenElement))
                            {
                                newToken = tokenElement.GetString();
                            }
                            if (dataElement.TryGetProperty("refreshToken", out JsonElement refreshTokenElement))
                            {
                                newRefreshToken = refreshTokenElement.GetString();
                            }
                        }
                        else
                        {
                            if (root.TryGetProperty("token", out JsonElement directToken))
                            {
                                newToken = directToken.GetString();
                            }
                            if (root.TryGetProperty("refreshToken", out JsonElement directRefreshToken))
                            {
                                newRefreshToken = directRefreshToken.GetString();
                            }
                        }

                        // Update session
                        if (!string.IsNullOrEmpty(newToken))
                        {
                            HttpContext.Session.SetString("AuthToken", newToken);
                        }
                        if (!string.IsNullOrEmpty(newRefreshToken))
                        {
                            HttpContext.Session.SetString("RefreshToken", newRefreshToken);
                        }
                    }

                    return Content(responseContent, "application/json");
                }
                else
                {
                    // Clear session if refresh token is invalid
                    HttpContext.Session.Clear();
                    return StatusCode((int)response.StatusCode, responseContent);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi refresh token!", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult GetSessionInfo()
        {
            var token = HttpContext.Session.GetString("AuthToken");
            var refreshToken = HttpContext.Session.GetString("RefreshToken");
            var userInfo = HttpContext.Session.GetString("UserInfo");

            return Json(new
            {
                hasToken = !string.IsNullOrEmpty(token),
                hasRefreshToken = !string.IsNullOrEmpty(refreshToken),
                userInfo = !string.IsNullOrEmpty(userInfo) ? JsonSerializer.Deserialize<JsonElement>(userInfo) : (JsonElement?)null
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

