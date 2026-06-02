using HochuProectWebApp.Data;
using HochuProectWebApp.Data.UnitOfWork;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HochuProectWebApp.Services.EF_core
{
    public class AdvertisementEfService : IAdvertisementService
    {
        private ILogger<AdvertisementEfService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public AdvertisementEfService(ILogger<AdvertisementEfService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<IServiceResult<List<Advertisement>>> GetAllAdvertisements()
        {
            var advertisements = await _unitOfWork.Advertisements.GetAllAsync();

            if (advertisements.Count == 0)
            {
                _logger.LogWarning($"{nameof(GetAllAdvertisements)}: объявления не найдены");
                return ServiceResult<List<Advertisement>>.Fail("Объявления не найдены");
            }

            return ServiceResult<List<Advertisement>>.Success(advertisements);
        }

        public async Task <IServiceResult<List<Advertisement>>> GetAdvertisementsByCategory(string categoryName)
        {

            var advertisements = await _unitOfWork.Advertisements.FindAsync(a => a.Category.Name == categoryName);

            if (advertisements.Count == 0)
            {
                _logger.LogWarning($"{nameof(GetAllAdvertisements)}: объявления не найдены");
                return ServiceResult<List<Advertisement>>.Fail("Объявления не найдены");
            }

            return ServiceResult<List<Advertisement>>.Success(advertisements);
        }


        public async Task<IServiceResult<Advertisement>> AddAdvertisement(
            string userEmail, string categoryName, Advertisement advertisement)
        {
            if (advertisement == null)
            {
                return ServiceResult<Advertisement>.Fail("Неверные данные в объявлении");
            }

            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return ServiceResult<Advertisement>.Fail("Пользователь не найден");
                }

                _logger.LogInformation("Добавление объявления по категории {categName}, пользователя с ID={id}.",
                categoryName, user.Id
                );

                var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                if (category == null)
                {
                    return ServiceResult<Advertisement>.Fail("Категория не найдена");
                }

                await _unitOfWork.BeginTransactionAsync();
                advertisement.Category = category;
                category.Advertisements.Add(advertisement);
                user.Advertisements.Add(advertisement);
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<Advertisement>.Success(advertisement);

            }
            catch
            {
                return ServiceResult<Advertisement>.Fail("Произошла ошибка при добавлении категории");
            }
        }
    }
}
