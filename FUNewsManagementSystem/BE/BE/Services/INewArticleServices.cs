using BE.DTO;
using BE.Models;

namespace BE.Services
{
    public interface INewArticleServices
    {
        IQueryable<NewsArticle> GetAllNewsArticles();
        Task<NewsArticle?> GetNewsArticleByIdAsync(string id);
        Task AddNewsArticleAsync(NewsArticle article);
        Task UpdateNewsArticleAsync(NewsArticle article);
        Task DeleteNewsArticleAsync(string id);
        IQueryable<ActiveNews> GetActiveNews();
    }
}
