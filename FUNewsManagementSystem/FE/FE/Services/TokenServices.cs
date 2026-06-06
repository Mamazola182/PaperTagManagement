using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
namespace FE.Services
{
    public class TokenServices
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public TokenServices(IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public string? GetAccessToken() => Session.GetString("AuthToken");
        public string? GetRefreshToken() => Session.GetString("RefreshToken");

        public void SetTokens(string accessToken, string refreshToken)
        {
            Session.SetString("AuthToken", accessToken);
            Session.SetString("RefreshToken", refreshToken);
        }

        public bool IsTokenExpiredSoon()
        {
            var token = GetAccessToken();
            if (string.IsNullOrEmpty(token)) return true;

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var remaining = jwt.ValidTo.ToUniversalTime() - DateTime.UtcNow;
            return remaining.TotalSeconds < 60; 
        }

        public async Task RefreshTokenAsync()
        {
            var refreshToken = GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
                return;

            var content = JsonContent.Create(refreshToken);

            var response = await _httpClient.PostAsync(
                "https://localhost:7135/api/auth/refreshToken",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<RefreshResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var newAccessToken = result?.Data?.AccessToken;
                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    Session.SetString("AuthToken", newAccessToken);
                }
            }
        }
    }

    public class RefreshResponse
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public RefreshData? Data { get; set; }
    }

    public class RefreshData
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}

