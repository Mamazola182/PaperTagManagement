using BE.Models;
using Microsoft.EntityFrameworkCore;

namespace BE.DataAccess
{
    public class TagDASqlServer : ITagDA
    {
        private readonly FunewsManagementContext _context;

        public TagDASqlServer(FunewsManagementContext context)
        {
            _context = context;
        }

        public IQueryable<Tag> GetAll()
        {
            return _context.Tags.Include(c=>c.NewsArticles).AsQueryable();
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tags.Include(c=>c.NewsArticles).FirstOrDefaultAsync(b => b.TagId == id);
        }

        public async Task AddAsync(Tag acc)
        {
            await _context.Tags.AddAsync(acc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tag acc)
        {
            _context.Tags.Update(acc);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tag = await GetByIdAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Boolean> IsTagUsingAsync(int id)
        {
            return await _context.NewsArticles.AnyAsync(news => news.Tags.Any(tag => tag.TagId == id));
        }
        public async Task<List<NewsArticle>> GetAllNewsForTag(int id)
        {
            var news = await _context.NewsArticles.Include(a => a.Tags)
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .Where(a => a.Tags.Any(t=>t.TagId == id)).ToListAsync();
            
            return news;
        }
    }
}
