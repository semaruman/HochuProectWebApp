using HochuProectWebApp.DTOs.User;
using HochuProectWebApp.Models;
using System.Security.Claims;

namespace HochuProectWebApp.Services.Interfaces
{
    public interface IUserService
    {
        Task<IServiceResult<User>> RegisterUser(UserRegisterDto model);

        Task<IServiceResult<ClaimsIdentity>> LoginUser(UserLoginDto model);
    }
}
