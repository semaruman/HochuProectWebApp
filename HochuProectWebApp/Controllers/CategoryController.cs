using HochuProectWebApp.Models;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HochuProectWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private ICategoryService _categoryService;
        private ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategoryNames()
        {
            var result = await _categoryService.GetCategoryNames();
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddCategory([FromQuery] string categoryName)
        {
            

            var result = await _categoryService.AddCategory(categoryName);
            if (result.IsSuccess)
            {
                _logger.LogInformation($"Категория '{categoryName}' добавлена успешно!");
                return Ok(new {Message = $"Категория '{categoryName}' добавлена успешно!"});
            }
            else
            {
                _logger.LogWarning("Ошибка при добавлении категории");
                return BadRequest(result.ErrorMessage);
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveCategory(string categoryName)
        {
            var result = await _categoryService.RemoveCategory(categoryName);
            if (result.IsSuccess)
            {
                return Ok($"Категория '{categoryName}' удалена");
            }
            else
            {
                return BadRequest(result.ErrorMessage);
            }
        }
    }
}
