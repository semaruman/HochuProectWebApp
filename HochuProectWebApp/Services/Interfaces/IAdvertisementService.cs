using HochuProectWebApp.Models;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface IAdvertisementService
    {
        public List<Advertisement> GetAllAdvertisements();

        public List<Advertisement> GetAdvertisementsByCategory(string categoryName);

        public Advertisement GetAdvertisementById(int id);

        public bool AddAdvertisement(Advertisement advertisement);

        public bool RemoveAdvertisementById(int id);


    }
}
