using BE.DTO;
using BE.Models;
using Microsoft.EntityFrameworkCore;

namespace BE.DataAccess
{
    public class NewsArticleDASqlServer : INewsArticleDA
    {
        private readonly FunewsManagementContext _context;

        public NewsArticleDASqlServer(FunewsManagementContext context)
        {
            _context = context;
        }
        public IQueryable<NewsArticle> GetAll()
        {
            return _context.NewsArticles
                .Include(a => a.Tags)
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .AsQueryable();
        }
        public async Task<NewsArticle?> GetByIdAsync(string id)
        {
            return await _context.NewsArticles
                .Include(a => a.Tags)
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(a => a.NewsArticleId == id);
        }
        public async Task AddAsync(NewsArticle article)
        {
            if (article.Tags != null && article.Tags.Any())
            {
                foreach (var tag in article.Tags)
                {
                    _context.Attach(tag);
                }
            }
            await _context.NewsArticles.AddAsync(article);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(NewsArticle article)
        {
            _context.NewsArticles.Update(article); 
            await _context.SaveChangesAsync(); 
        }

        public async Task DeleteAsync(string id)
        {
            var article = await _context.NewsArticles
                .Include(a => a.Tags)
                .FirstOrDefaultAsync(a => a.NewsArticleId == id);
            if (article != null)
            {
                article.Tags.Clear();
                _context.NewsArticles.Remove(article);
                await _context.SaveChangesAsync();
            }
        }
        public IQueryable<ActiveNews> GetActiveArticle()
        {
            var news = _context.NewsArticles
                .Include(a => a.Tags)
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .Where(a => a.NewsStatus == true)
                .Select(a => new ActiveNews
                {
                    NewsArticleId = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    Headline = a.Headline,
                    CreatedDate = a.CreatedDate,
                    ModifiedDate = a.ModifiedDate,
                    NewsContent = a.NewsContent,
                    NewsSource = a.NewsSource,
                    NewsStatus = a.NewsStatus,
                    Category = a.Category,         
                    CreatedBy = a.CreatedBy,       
                    UpdatedBy = _context.SystemAccounts.FirstOrDefault(acc => acc.AccountId == a.UpdatedById),      
                    Tags = a.Tags                  
                })
                .AsQueryable();

            return news;
        }
    }
}
