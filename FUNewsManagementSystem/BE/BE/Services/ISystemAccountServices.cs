using BE.Models;

namespace BE.Services
{
    public interface ISystemAccountServices
    {
        IQueryable<SystemAccount> GetAllSystemAccounts();
        Task<SystemAccount?> GetSystemAccountByIdAsync(int id);
        Task<SystemAccount?> GetSystemAccountByEmailAsync(string email);
        Task AddSystemAccountAsync(SystemAccount acc);
        Task UpdateSystemAccountAsync(SystemAccount acc);
        Task DeleteSystemAccountAsync(int id);
        Task<Boolean> IsAcountCreateNew(int id);
    }
}
