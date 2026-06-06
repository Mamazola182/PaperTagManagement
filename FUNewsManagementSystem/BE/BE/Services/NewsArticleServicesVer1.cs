using BE.DataAccess;
using BE.DTO;
using BE.Models;

namespace BE.Services
{
    public class NewsArticleServicesVer1:INewArticleServices
    {
        private readonly INewsArticleDA _newsArticleDA;

        public NewsArticleServicesVer1(INewsArticleDA newsArticleDA)
        {
            _newsArticleDA = newsArticleDA;
        }

        public IQueryable<NewsArticle> GetAllNewsArticles()
        {
            return _newsArticleDA.GetAll();
        }

        public async Task<NewsArticle?> GetNewsArticleByIdAsync(string id)
        {
            return await _newsArticleDA.GetByIdAsync(id);
        }

        public async Task AddNewsArticleAsync(NewsArticle article)
        {
            if (article == null)
                throw new ArgumentNullException(nameof(article));
            await _newsArticleDA.AddAsync(article);
        }

        public async Task UpdateNewsArticleAsync(NewsArticle article)
        {
            if (article == null)
                throw new ArgumentNullException(nameof(article));
            await _newsArticleDA.UpdateAsync(article);
        }

        public async Task DeleteNewsArticleAsync(string id)
        {
            await _newsArticleDA.DeleteAsync(id);
        }
        public IQueryable<ActiveNews> GetActiveNews()
        {
            return _newsArticleDA.GetActiveArticle();
        }
    }
}
