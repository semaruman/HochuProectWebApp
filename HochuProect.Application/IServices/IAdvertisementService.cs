using HochuProect.Domain.Entities;

namespace HochuProect.Application.IServices
{
    public interface IAdvertisementService
    {
        public Task<IServiceResult<List<Advertisement>>> GetAllAdvertisements();

        public Task<IServiceResult<List<Advertisement>>> GetAdvertisementsByCategory(string categoryName);


        public Task<IServiceResult<Advertisement>> AddAdvertisement(string userEmail, string categoryName, Advertisement advertisement);
    }
}
