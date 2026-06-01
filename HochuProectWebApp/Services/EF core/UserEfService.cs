using HochuProectWebApp.Data;
using HochuProectWebApp.Data.UnitOfWork;
using HochuProectWebApp.DTOs.User;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HochuProectWebApp.Services.EF_core
{
    public class UserEfService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserEfService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult<User>> RegisterUser(UserRegisterDto model)
        {
            if (_unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == model.Email) != null)
            {
                return ServiceResult<User>.Fail($"Пользователь с email: {model.Email} уже существует");
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                Role = "user"
            };

            _unitOfWork.Users.Add(user);
            await _unitOfWork.SavaChangesAsync();
            return ServiceResult<User>.Success(user);
        }

        public async Task<IServiceResult<ClaimsIdentity>> LoginUser(UserLoginDto model)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return ServiceResult<ClaimsIdentity>.Fail("Пользователь не зарегистрирован");
            }

            if (user.Password != model.Password)
            {
                return ServiceResult<ClaimsIdentity>.Fail("Неверный пароль");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

            return ServiceResult<ClaimsIdentity>.Success(claimsIdentity);
        }
    }
}
