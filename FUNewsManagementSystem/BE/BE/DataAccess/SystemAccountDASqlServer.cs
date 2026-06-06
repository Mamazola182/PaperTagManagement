using BE.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BE.DataAccess
{
    public class SystemAccountDASqlServer:ISystemAccountDA
    {
        private readonly FunewsManagementContext _context;

        public SystemAccountDASqlServer(FunewsManagementContext context)
        {
            _context = context;
        }

        public IQueryable<SystemAccount> GetAll()
        {
            return _context.SystemAccounts.AsQueryable();
        }

        public async Task<SystemAccount?> GetByIdAsync(int id)
        {
            return await _context.SystemAccounts.Include(acc=>acc.NewsArticles).FirstOrDefaultAsync(b => b.AccountId == id);
        }

        public async Task<SystemAccount?> GetByEmailAsync(string email)
        {
            return await _context.SystemAccounts.FirstOrDefaultAsync(b => b.AccountEmail == email);
        }
        public async Task AddAsync(SystemAccount acc)
        {
            await _context.SystemAccounts.AddAsync(acc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SystemAccount acc)
        {
            _context.SystemAccounts.Update(acc);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var systemAccount = await GetByIdAsync(id);
            if (systemAccount != null)
            {
                _context.SystemAccounts.Remove(systemAccount);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Boolean> IsAcountCreateNew(int id)
        {
            return await _context.NewsArticles.AnyAsync(b => b.CreatedById == id);
        }

    }
}
