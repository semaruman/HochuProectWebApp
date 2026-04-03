using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HochuProectWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private IAdvertisementService _advertisementService;

        public AdvertisementController(IAdvertisementService advertisementService)
        {
            _advertisementService = advertisementService;
        }

        [HttpGet("all-advertisement")]
        public IActionResult GetAllAdvertisement()
        {
            var advertisements = _advertisementService.GetAllAdvertisements();
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

        [HttpPost("{userId}/advertisements/add")]
        public IActionResult AddAdvertisement(
            [FromRoute] int userId,
            [FromQuery] string categoryName,
            [FromBody] Advertisement advertisement)
        {
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
