using HochuProectWebApp.Models;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface ICategoryService
    {
        public List<Category> GetCategories();

        public bool AddCategory(Category category);

        public bool RemoveCategory(string categoryName);
    }
}
