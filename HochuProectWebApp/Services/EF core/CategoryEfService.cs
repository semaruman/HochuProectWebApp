using HochuProectWebApp.Data;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HochuProectWebApp.Services.EF_core
{
    public class CategoryEfService : ICategoryService
    {
        public List<Category> GetCategories()
        {
            using var dbContext = new ApplicationDbContext();

            return dbContext.Categories.AsNoTracking().ToList();
        }

        public bool AddCategory(Category category)
        {
            using var dbContext = new ApplicationDbContext();

            if (dbContext.Categories.Select(c => c.Name).Contains(category.Name))
            {
                return false;
            }
            else
            {
                dbContext.Categories.Add(category);
                dbContext.SaveChanges();
                return true;
            }
        }

        public bool RemoveCategory(string categoryName)
        {
            using var dbContext = new ApplicationDbContext();

            var category = dbContext.Categories.FirstOrDefault(c => c.Name == categoryName);

            if (category == null)
            {
                return false;
            }
            else
            {
                dbContext.Categories.Remove(category);
                return true;
            }
        }
    }
}
