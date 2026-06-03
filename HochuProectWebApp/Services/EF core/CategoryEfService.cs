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

        public async Task<IServiceResult<bool>> AddCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                _logger.LogWarning("Отсутствует название категории");
                return ServiceResult<bool>.Fail("Отсутствует название категории");
            }

            var foundCategory = _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);

            if (foundCategory != null)
            {
                return ServiceResult<bool>.Fail("Категория уже существует");
            }
            else
            {
                _unitOfWork.Categories.Add(new Category { Name = categoryName});
                
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
        }

        public async Task<IServiceResult<bool>> RemoveCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return ServiceResult<bool>.Fail("Отсутствует название категории");
            }

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);

            if (category == null)
            {
                return ServiceResult<bool>.Fail("Категория не найдена");
            }
            else
            {
                _unitOfWork.Categories.Remove(category);
                return ServiceResult<bool>.Success(true);
            }
        }
    }
}
