using HochuProectWebApp.Models;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface IAdvertisementService
    {
        public Task<IServiceResult<List<Advertisement>>> GetAllAdvertisements();

        public Task<IServiceResult<List<Advertisement>>> GetAdvertisementsByCategory(string categoryName);


        public Task<IServiceResult<Advertisement>> AddAdvertisement(string userEmail, string categoryName, Advertisement advertisement);
    }
}
