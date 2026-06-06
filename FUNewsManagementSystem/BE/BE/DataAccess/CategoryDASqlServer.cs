using BE.Models;
using Microsoft.EntityFrameworkCore;
using BE.DTO;
namespace BE.DataAccess
{
    public class CategoryDASqlServer:ICategoryDA
    {
        private readonly FunewsManagementContext _context;

        public CategoryDASqlServer(FunewsManagementContext context)
        {
            _context = context;
        }

        public IQueryable<CategoryDTO> GetAll()
        {
            var categories = _context.Categories.ToList();
            var result = new List<CategoryDTO>();
            foreach (var c in categories)
            {
                var articleCount = _context.NewsArticles.CountAsync(b => b.CategoryId == c.CategoryId).Result;
                result.Add(new CategoryDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryDesciption = c.CategoryDesciption,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive,
                    ArticleCount = articleCount
                });
            }
            return result.AsQueryable();
        }

        public async Task<CategoryDTO?> GetDTOByIdAsync(int id)
        {
            var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return null;

            var articleCount = await _context.NewsArticles
                .CountAsync(a => a.CategoryId == id);

            return new CategoryDTO
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                CategoryDesciption = category.CategoryDesciption,
                ParentCategoryId = category.ParentCategoryId,
                IsActive = category.IsActive,
                ArticleCount = articleCount
            };
        }
        public async Task<Category?> GetByIdAsync(int id)
        {
            return  await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }
        public async Task AddAsync(Category cat)
        {
            await _context.Categories.AddAsync(cat);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category cat)
        {
            _context.Categories.Update(cat);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var cat = await GetByIdAsync(id);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Boolean> IsCategoryUsing(int id)
        {
            return await _context.NewsArticles.AnyAsync(b => b.CategoryId == id);
        }
        public async Task<int> CountArticlesInCategory(int id)
        {
            return await _context.NewsArticles.CountAsync(b => b.CategoryId == id);
        }
        public IQueryable<Category> GetActive()
        {
            return _context.Categories.Where(c => c.IsActive == true).AsQueryable();
        }
        public async Task<List<NewsArticle>> GetArticlesInCategory(int id)
        {
            var articles = await _context.NewsArticles
                .Include(a => a.Tags)
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .Where(a => a.CategoryId == id).ToListAsync();
            return articles;
        }
    }
}
