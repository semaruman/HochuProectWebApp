using HochuProectWebApp.Data;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HochuProectWebApp.Services.EF_core
{
    public class UserEfService : IUserService
    {
        public User GetUserById(int userId)
        {
            using var dbContext = new ApplicationDbContext();
            return dbContext.Users.Find(userId);
        }

        public User GetUserByEmail(string email)
        {
            using var dbContext = new ApplicationDbContext();
            return dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Email == email);
        }

        public bool AddUser(User user)
        {
            using var dbContext = new ApplicationDbContext();
            try
            {
                if (dbContext.Users.Select(u => u.Email).Contains(user.Email))
                {
                    return false;
                }
                else
                {
                    user.CreatedDate = user.CreatedDate.ToUniversalTime();
                    dbContext.Users.Add(user);
                    dbContext.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveUser(int userId)
        {
            using var dbContext = new ApplicationDbContext();
            var user = dbContext.Users.Find(userId);

            if (user == null)
            {
                return false;
            }
            else
            {
                dbContext.Remove(user);
                dbContext.SaveChanges();
                return true;
            }
        }
    }
}
