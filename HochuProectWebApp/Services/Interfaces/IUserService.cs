using HochuProectWebApp.Models;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface IUserService
    {
        public User GetUserById(int userId);

        public User GetUserByEmail(string email);

        public bool AddUser(User user);

        public bool RemoveUser(int userId);
    }
}
