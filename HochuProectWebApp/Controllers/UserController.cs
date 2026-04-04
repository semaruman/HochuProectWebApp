using HochuProectWebApp.DTOs.User;
using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public IActionResult RegisterUser([FromBody] UserRegisterDto model)
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

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(string returnUrl, [FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var user = _userService.GetUserByEmail(model.Email);

            if (user == null)
            {
                return Unauthorized(new { Error = "Пользователь не зарегистрирован" });
            }
            
            if (user.Password != model.Password)
            {
                return Unauthorized(new { Error = "Не верный пароль" });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, "user")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return Redirect(returnUrl ?? "/");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogoutUser()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/login");
        }
    }
}
