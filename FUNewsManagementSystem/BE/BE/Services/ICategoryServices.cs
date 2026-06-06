using BE.Models;
using BE.DTO;
namespace BE.Services
{
    public interface ICategoryServices
    {
        IQueryable<CategoryDTO> GetAllCategories();
        Task<CategoryDTO?> GetCategoryDTOByIdAsync(int id);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task AddCategoryAsync(Category cat);
        Task UpdateCategoryAsync(Category cat);
        Task DeleteCategoryAsync(int id);
        Task<Boolean> IsCategoryUsing(int id);
        Task<int> CountArticlesInCategory(int id);
        IQueryable<Category> GetAllActiveCategories();
        Task<List<NewsArticle>> GetArticlesInCategory(int id);
    }
}
