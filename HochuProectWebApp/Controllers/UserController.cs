using HochuProectWebApp.DTOs.User;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HochuProectWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public IActionResult RegisterUser([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_userService.GetUserByEmail(model.Email) != null)
            {
                return BadRequest(new {Error = "Пользователь с таким email уже существует"});
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                Role = "user"
            };

            if (_userService.AddUser(user))
            {
                return Ok(new {Message = $"Пользователь {model.Name} зарегистрирован"});
            }
            else
            {
                return BadRequest(new {Error = "Ошибка регистрации"});
            }
        }
    }
}
