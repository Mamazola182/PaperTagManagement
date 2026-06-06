using BE.Models;

namespace BE.DataAccess
{
    public interface ITagDA
    {
        IQueryable<Tag> GetAll();
        Task<Tag?> GetByIdAsync(int id);
        Task AddAsync(Tag acc);
        Task UpdateAsync(Tag acc);
        Task DeleteAsync(int id);
        Task<Boolean> IsTagUsingAsync(int id);
        Task<List<NewsArticle>> GetAllNewsForTag(int id);
    }
}
