using BE.DataAccess;
using BE.Models;

namespace BE.Services
{
    public class TagServicesVer1: ITagServices
    {
         private readonly ITagDA _tag;

        public TagServicesVer1(ITagDA tag)
        {
            _tag = tag;
        }

        public IQueryable<Tag> GetAllTags()
        {
            return _tag.GetAll();
        }
        public async Task<Tag?> GetTagByIdAsync(int id)
        {
            return await _tag.GetByIdAsync(id);
        }
        public async Task AddTagAsync(Tag acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            await _tag.AddAsync(acc);
        }
        public async Task UpdateTagAsync(Tag acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            await _tag.UpdateAsync(acc);
        }
        public async Task DeleteTagAsync(int id)
        {
            await _tag.DeleteAsync(id);
        }
        public async Task<Boolean> IsTagUsingAsync(int id)
        {
            return await _tag.IsTagUsingAsync(id);
        }
        public async Task<List<NewsArticle>> GetAllNewsForTag(int id)
        {
            return await _tag.GetAllNewsForTag(id);
        }
    }
}
