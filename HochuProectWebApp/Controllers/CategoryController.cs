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
        public IActionResult GetCategories()
        {
            return Ok(_categoryService.GetCategories().Select(c => c.Name));
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add")]
        public IActionResult AddCategory([FromQuery] string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                _logger.LogWarning("Отсутствует название категории");
                return BadRequest(new {Error = "Отсутствует название категории"});
            }

            var category = new Category { Name = categoryName };
            if (_categoryService.AddCategory(category))
            {
                _logger.LogInformation($"Категория '{categoryName}' добавлена успешно!");
                return Ok(new {Message = $"Категория '{categoryName}' добавлена успешно!"});
            }
            else
            {
                _logger.LogWarning("Ошибка при добавлении категории");
                return BadRequest(new { Error = "Ошибка при добавлении категории" });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPost("remove")]
        public IActionResult RemoveCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                return BadRequest(new { Error = "Отсутствует название категории" });
            }

            if (_categoryService.RemoveCategory(categoryName))
            {
                return Ok(new { Message = $"Категория '{categoryName}' удалена" });
            }
            else
            {
                return BadRequest(new { Error = "Ошибка при удалении категории" });
            }
        }
    }
}
