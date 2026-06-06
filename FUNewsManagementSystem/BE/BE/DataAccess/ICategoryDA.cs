using BE.Models;
using BE.DTO; 
namespace BE.DataAccess
{
    public interface ICategoryDA
    {
        IQueryable<CategoryDTO> GetAll();
        Task<CategoryDTO?> GetDTOByIdAsync(int id);
        Task<Category?> GetByIdAsync(int id);
        Task AddAsync(Category cat);
        Task UpdateAsync(Category cat);
        Task DeleteAsync(int id);
        Task<Boolean> IsCategoryUsing(int id);
        Task<int> CountArticlesInCategory(int id);
        IQueryable<Category> GetActive();
        Task<List<NewsArticle>> GetArticlesInCategory(int id);
    }
}
