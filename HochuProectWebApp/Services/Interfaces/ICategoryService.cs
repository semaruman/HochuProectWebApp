using HochuProectWebApp.Models;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface ICategoryService
    {
        public Task<IServiceResult<List<string>>> GetCategoryNames();

        public Task<IServiceResult<bool>> AddCategory(string categoryName);

        public bool RemoveCategory(string categoryName);
    }
}
