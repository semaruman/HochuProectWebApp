using HochuProect.Domain.Entities;
using HochuProect.Application.DTOs.User;
using System.Security.Claims;

namespace HochuProect.Application.IServices
{
    public interface IUserService
    {
        Task<IServiceResult<User>> RegisterUser(UserRegisterDto model);

        Task<IServiceResult<ClaimsIdentity>> LoginUser(UserLoginDto model);
    }
}
