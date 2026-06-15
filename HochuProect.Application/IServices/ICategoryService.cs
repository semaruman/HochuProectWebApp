using HochuProect.Domain.Entities;

namespace HochuProect.Application.IServices
{
    public interface ICategoryService
    {
        public Task<IServiceResult<List<string>>> GetCategoryNames();

        public Task<IServiceResult<bool>> AddCategory(string categoryName);

        public Task<IServiceResult<bool>> RemoveCategory(string categoryName);
    }
}
