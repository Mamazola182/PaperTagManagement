using BE.Models;

namespace BE.Services
{
    public interface ITagServices
    {
        IQueryable<Tag> GetAllTags();
        Task<Tag?> GetTagByIdAsync(int id);
        Task AddTagAsync(Tag acc);
        Task UpdateTagAsync(Tag acc);
        Task DeleteTagAsync(int id);
        Task<Boolean> IsTagUsingAsync(int id);
        Task<List<NewsArticle>> GetAllNewsForTag(int id);
    }
}
