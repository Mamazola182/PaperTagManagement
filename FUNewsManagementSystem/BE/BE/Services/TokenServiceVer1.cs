using Microsoft.IdentityModel.Tokens;
using BE.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BE.Services
{
    public class TokenServiceVer1 : ITokenServices
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly ISystemAccountServices _userService;
        private readonly FunewsManagementContext _context;
        public TokenServiceVer1(IConfiguration config, ISystemAccountServices userService, FunewsManagementContext context, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _userService = userService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private (SymmetricSecurityKey Key, IConfigurationSection Jwt) GetJwtSettings()
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = jwtSettings["Key"] ?? throw new ArgumentNullException("Jwt:Key is missing");
            return (new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), jwtSettings);
        }

        public async Task<string> CreateAccessToken(string email)
        {
            var user = await _userService.GetSystemAccountByEmailAsync(email);
            var (key, jwtSettings) = GetJwtSettings();

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var role = user.AccountRole == 1 ? "Staff" : "Lecturer";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenMinutes"] ?? "30")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> CreateAccessTokenAdmin()
        {
            var (key, jwtSettings) = GetJwtSettings();
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, _config["DefaultAdmin:Email"]),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenMinutes"] ?? "30")),
                signingCredentials: creds
            );

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }

        public async Task<string> CreateRefreshToken(string email)
        {
            var user = await _userService.GetSystemAccountByEmailAsync(email);
            var (key, jwtSettings) = GetJwtSettings();

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var role = user.AccountRole == 1 ? "Staff" : "Lecturer";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["RefreshTokenMinutes"] ?? "10080")), // default 7 days
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> CreateRefreshTokenAdmin()
        {
            var (key, jwtSettings) = GetJwtSettings();
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["RefreshTokenMinutes"] ?? "10080")),
                signingCredentials: creds
            );

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }

        public async Task<string> RefreshToken(string refreshToken)
        {
            var (key, jwtSettings) = GetJwtSettings();
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = key
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (role == "Admin")
                    return await CreateAccessTokenAdmin();
                else if (role is "Staff" or "Lecturer")
                {
                    var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                    if (email == null) return "email null";
                    return await CreateAccessToken(email);
                }

                return "lỗi";
            }
            catch (Exception ex) 
            {
                Console.WriteLine("❌ ValidateToken failed: " + ex.Message);
                return "validate fail: " + ex.Message;
            }
        }
        public string GetEmailFromToken()
        {
            
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                throw new Exception("Authorization header missing or invalid.");
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "Role")?.Value;
            if (role=="Admin") return "Admin";
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "email")?.Value;
            if (string.IsNullOrEmpty(email))
                throw new Exception("Email claim not found in token.");
            return email;
        }
        public short GetIdFromToken()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                throw new Exception("Authorization header missing or invalid.");
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "Role")?.Value;
            if (role == "Admin") return 0;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "email")?.Value;
            if (string.IsNullOrEmpty(email))
                throw new Exception("Email claim not found in token.");
            var account = _context.SystemAccounts.FirstOrDefault(a => a.AccountEmail == email);
            if (account == null)
                throw new Exception("Account not found.");
            return account.AccountId;
        }
    }
}
