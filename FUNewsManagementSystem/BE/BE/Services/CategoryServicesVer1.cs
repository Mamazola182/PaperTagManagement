using BE.DataAccess;
using BE.Models;
using BE.DTO; 
using Microsoft.EntityFrameworkCore;

namespace BE.Services
{
    public class CategoryServicesVer1:ICategoryServices
    {
        private readonly ICategoryDA _category;

        public CategoryServicesVer1(ICategoryDA category)
        {
            _category = category;
        }

        public IQueryable<CategoryDTO> GetAllCategories()
        {
            return _category.GetAll();
        }
        public async Task<CategoryDTO?> GetCategoryDTOByIdAsync(int id)
        {
            return await _category.GetDTOByIdAsync(id);
        }
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _category.GetByIdAsync(id);
        }
        public async Task AddCategoryAsync(Category acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            await _category.AddAsync(acc);
        }
        public async Task UpdateCategoryAsync(Category acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            await _category.UpdateAsync(acc);
        }
        public async Task DeleteCategoryAsync(int id)
        {
            await _category.DeleteAsync(id);
        }
        public async Task<Boolean> IsCategoryUsing(int id)
        {
            return await _category.IsCategoryUsing(id);
        }
        public async Task<int> CountArticlesInCategory(int id)
        {
            return await _category.CountArticlesInCategory(id);
        }
        public IQueryable<Category> GetAllActiveCategories()
        {
            return _category.GetActive();
        }
        public async Task<List<NewsArticle>> GetArticlesInCategory(int id)
        {
            return await _category.GetArticlesInCategory(id);
        }
    }
}
