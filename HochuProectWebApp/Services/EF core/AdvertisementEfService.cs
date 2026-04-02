using HochuProectWebApp.Data;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HochuProectWebApp.Services.EF_core
{
    public class AdvertisementEfService : IAdvertisementService
    {
        public List<Advertisement> GetAllAdvertisements()
        {
            using var dbContext = new ApplicationDbContext();

            return dbContext.Advertisements.AsNoTracking().ToList();
        }

        public List<Advertisement> GetAdvertisementsByCategory(string categoryName)
        {
            using var dbContext = new ApplicationDbContext();

            return dbContext.Advertisements.AsNoTracking().Where(a => a.Category.Name  == categoryName).ToList();
        }

        public Advertisement GetAdvertisementById(int id)
        {
            using var dbContext = new ApplicationDbContext();

            return dbContext.Advertisements.Find(id);
        }

        public bool AddAdvertisement(Advertisement advertisement)
        {
            try
            {
                using var dbContext = new ApplicationDbContext();
                dbContext.Advertisements.Add(advertisement);
                dbContext.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveAdvertisementById(int id)
        {
            try
            {
                using var dbContext = new ApplicationDbContext();
                var adv = dbContext.Advertisements.Find(id);

                if (adv != null)
                {
                    dbContext.Advertisements.Remove(adv);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool AddAdvertisement(Advertisement advertisement, int userId, string categoryName)
        {
            using var dbContext = new ApplicationDbContext();

            var user = dbContext.Users.Find(userId);
            var category = dbContext.Categories.FirstOrDefault( c => c.Name == categoryName);

            if (category == null || user == null)
            {
                return false;
            }


            advertisement.User = user;
            advertisement.Category = category;

            dbContext.SaveChanges();

            return true;
        }
    }
}
