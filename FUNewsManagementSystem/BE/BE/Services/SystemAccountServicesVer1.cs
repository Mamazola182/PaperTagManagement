using BE.DataAccess;
using BE.Models;

namespace BE.Services
{
    public class SystemAccountServicesVer1:ISystemAccountServices
    {
        private readonly ISystemAccountDA _systemaccount;

        public SystemAccountServicesVer1(ISystemAccountDA systemaccount)
        {
            _systemaccount = systemaccount;
        }

        public IQueryable<SystemAccount> GetAllSystemAccounts()
        {
            return _systemaccount.GetAll();
        }
        public async Task<SystemAccount?> GetSystemAccountByIdAsync(int id)
        {
            return await _systemaccount.GetByIdAsync(id);
        }
        public async Task<SystemAccount?> GetSystemAccountByEmailAsync(string email)
        {
            return await _systemaccount.GetByEmailAsync(email);
        }
        public async Task AddSystemAccountAsync(SystemAccount acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            await _systemaccount.AddAsync(acc);
        }
        public async Task UpdateSystemAccountAsync(SystemAccount acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            await _systemaccount.UpdateAsync(acc);
        }
        public async Task DeleteSystemAccountAsync(int id)
        {
            await _systemaccount.DeleteAsync(id);
        }
        public async Task<Boolean> IsAcountCreateNew(int id)
        {
            return await _systemaccount.IsAcountCreateNew(id);
        }
    }
}
