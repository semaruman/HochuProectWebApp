using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HochuProectWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private IAdvertisementService _advertisementService;
        private IUserService _userService;
        private ILogger<AdvertisementController> _logger;

        public AdvertisementController(
            IAdvertisementService advertisementService, 
            IUserService userService, 
            ILogger<AdvertisementController> logger)
        {
            _advertisementService = advertisementService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("all-advertisement")]
        public IActionResult GetAllAdvertisement()
        {
            var advertisements = _advertisementService.GetAllAdvertisements();
            if (advertisements.Count == 0)
            {
                _logger.LogWarning($"{nameof(GetAllAdvertisement)}: объявления не найдены");
                return NotFound(new { Error = "Объявления не найдены" });
            }
            return Ok(advertisements.Select(a =>
            {
                return new Advertisement
                {
                    Id = a.Id,
                    Title = a.Title,
                    Photos = a.Photos,
                    Description = a.Description,
                    UserId = a.UserId,
                    CategoryId = a.CategoryId,
                };
            }));
        }

        [HttpGet("{categoryName}/advertisements")]
        public IActionResult GetAdvertisementsByCategory(string categoryName)
        {
            var advertisements = _advertisementService.GetAdvertisementsByCategory(categoryName);
            if (advertisements.Count == 0)
            {
                return NotFound(new { Error = "Объявления не найдены" });
            }
            return Ok(advertisements.Select(a =>
            {
                return new Advertisement
                {
                    Id = a.Id,
                    Title = a.Title,
                    Photos = a.Photos,
                    Description = a.Description,
                    UserId = a.UserId,
                    CategoryId = a.CategoryId,
                };
            }));
        }

        [Authorize]
        [HttpPost("advertisements/add")]
        public IActionResult AddAdvertisement(
            [FromQuery] string categoryName,
            [FromBody] Advertisement advertisement)
        {
            string email = User.FindFirst(ClaimTypes.Email).Value;
            int userId = _userService.GetUserByEmail(email).Id;

            _logger.LogInformation("Добавление объявления по категории {categName}, пользователя с ID={id}.",
                categoryName, userId
                );
            if (advertisement == null)
            {
                return BadRequest(new { Error = "Некорректные данные" });
            }

            if (_advertisementService.AddAdvertisement(advertisement, userId, categoryName))
            {
                return Ok(new { message = "Объявление создано" });
            }
            else
            {
                return BadRequest(new { Error = "Ошибка добавления" });
            }
        }
    }
}
