using HochuProectWebApp.Models;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface ICategoryService
    {
        public Task<IServiceResult<List<string>>> GetCategoryNames();

        public bool AddCategory(Category category);

        public bool RemoveCategory(string categoryName);
    }
}
