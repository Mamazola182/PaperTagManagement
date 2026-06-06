using BE.DTO;
using BE.Models;

namespace BE.DataAccess
{
    public interface INewsArticleDA
    {
        IQueryable<NewsArticle> GetAll();
        Task<NewsArticle?> GetByIdAsync(string id);
        Task AddAsync(NewsArticle news);
        Task UpdateAsync(NewsArticle news);
        Task DeleteAsync(string id);
        IQueryable<ActiveNews> GetActiveArticle();
    }
}
