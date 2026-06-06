using BE.Models;

namespace BE.DataAccess
{
    public interface ISystemAccountDA
    {
        IQueryable<SystemAccount> GetAll();
        Task<SystemAccount?> GetByIdAsync(int id);
        Task<SystemAccount?> GetByEmailAsync(string email);
        Task AddAsync(SystemAccount acc);
        Task UpdateAsync(SystemAccount acc);
        Task DeleteAsync(int id);
        Task<Boolean> IsAcountCreateNew(int id);
    }
}
