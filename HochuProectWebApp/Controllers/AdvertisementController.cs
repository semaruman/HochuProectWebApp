using HochuProect.Application.IServices;
using HochuProect.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HochuProectWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private IAdvertisementService _advertisementService;
        private ILogger<AdvertisementController> _logger;

        public AdvertisementController(
            IAdvertisementService advertisementService, 
            ILogger<AdvertisementController> logger)
        {
            _advertisementService = advertisementService;
            _logger = logger;
        }

        [HttpGet("all-advertisement")]
        public async Task<IActionResult> GetAllAdvertisements()
        {
            var result = await _advertisementService.GetAllAdvertisements();
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Value);
        }

        [HttpGet("{categoryName}/advertisements")]
        public async Task<IActionResult> GetAdvertisementsByCategory(string categoryName)
        {
            var result = await _advertisementService.GetAdvertisementsByCategory(categoryName);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize]
        [HttpPost("advertisements/add")]
        public async Task<IActionResult> AddAdvertisement(
            [FromQuery] string categoryName,
            [FromBody] Advertisement advertisement)
        {
            string email = User.FindFirst(ClaimTypes.Email).Value;
            var result = await _advertisementService.AddAdvertisement(email, categoryName, advertisement);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Value);
        }
    }
}
