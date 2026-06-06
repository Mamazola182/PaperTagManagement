using BE.Models;
namespace BE.Services
{
    public interface ITokenServices
    {
        Task<string> CreateAccessToken(string username);
        Task<string> CreateAccessTokenAdmin();
        Task<string> CreateRefreshToken(string Email);
        Task<string> CreateRefreshTokenAdmin();
        Task<string> RefreshToken(string refreshToken);
        short GetIdFromToken();
        string GetEmailFromToken();
    }
}
