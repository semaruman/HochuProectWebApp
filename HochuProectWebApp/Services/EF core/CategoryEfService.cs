using HochuProectWebApp.Data;
using HochuProectWebApp.Data.UnitOfWork;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HochuProectWebApp.Services.EF_core
{
    public class CategoryEfService : ICategoryService
    {
        private ILogger<CategoryEfService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public CategoryEfService(ILogger<CategoryEfService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult<List<string>>> GetCategoryNames()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            if (categories == null || categories.Count == 0)
            {
                return ServiceResult<List<string>>.Fail("Категории не найдены");
            }
            
            var categoryNames = categories.Select(c => c.Name).ToList();

            return ServiceResult<List<string>>.Success(categoryNames);
        }

        public bool AddCategory(Category category)
        {
            using var dbContext = new ApplicationDbContext(null);

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
            using var dbContext = new ApplicationDbContext(null);

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
