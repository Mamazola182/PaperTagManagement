using BE.DTO;
using BE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BE.Controllers
{
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly ISystemAccountServices _systemAccountService;
        private readonly ITokenServices _tokenServices;
        private readonly IConfiguration _configuration;
        public AuthController(ISystemAccountServices systemAccountService, ITokenServices tokenServices, IConfiguration configuration)
        {
            _systemAccountService = systemAccountService;
            _tokenServices = tokenServices;
            _configuration = configuration;
        }
        [HttpPost("login")]
        public async Task<IActionResult> login([FromBody] Login log)
        {
            try
            {
                var email = _configuration["DefaultAdmin:Email"];
                var password = _configuration["DefaultAdmin:Password"];
                var user = _systemAccountService.GetSystemAccountByEmailAsync(log.Email).Result;
                if (email == log.Email && password == log.Password)
                {
                    var tokenAdmin = await _tokenServices.CreateAccessTokenAdmin();
                    var refreshTokenAdmin = await _tokenServices.CreateRefreshTokenAdmin();
                    return Ok(new
                    {
                        status = StatusCodes.Status200OK,
                        message = "Success",
                        data = new
                        {
                            token = tokenAdmin,
                            refreshToken = refreshTokenAdmin,
                        }
                    });
                }
                if (user == null || user.AccountPassword != log.Password)
                {
                    return Unauthorized(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        message = "Error",
                        error = "Invalid email or password."
                    });
                }
                var token = await _tokenServices.CreateAccessToken(user.AccountEmail);
                var refreshToken = await _tokenServices.CreateRefreshToken(user.AccountEmail);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    data = new
                    {
                        token = token,
                        refreshToken = refreshToken,
                        user = new
                        {
                            id = user.AccountId,
                            email = user.AccountEmail,
                            name = user.AccountName,
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }

        [HttpPost("refreshToken")]
        public async Task<IActionResult> refreshToken([FromBody] string refreshToken)
        {
            try
            {
                var newToken = await _tokenServices.RefreshToken(refreshToken);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    data = new
                    {
                        accessToken = newToken,
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }
    }
}
