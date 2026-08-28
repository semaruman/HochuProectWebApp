using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Domain.Entities;
using Web.Domain.ValueObjects;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Services;

[Authorize]
public class CreateModel(AppDbContext db, ICurrentUser currentUser) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = [];

    public class InputModel
    {
        [Required, MinLength(5)] public string Title { get; set; } = string.Empty;
        [Required, MinLength(20)] public string Description { get; set; } = string.Empty;
        [Required] public Guid CategoryId { get; set; }
        [Range(1, 100000000)] public decimal Price { get; set; }
        [Range(1, 3650)] public int DeliveryDays { get; set; } = 7;
    }

    public async Task OnGetAsync() => await Load();

    public async Task<IActionResult> OnPostAsync()
    {
        await Load();
        if (!ModelState.IsValid) return Page();
        var service = Service.Create(
            currentUser.UserId,
            Input.CategoryId,
            Input.Title,
            Input.Description,
            Money.Rub(Input.Price),
            Input.DeliveryDays,
            DateTime.UtcNow);
        service.Publish(DateTime.UtcNow);
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return RedirectToPage("/Services/Details", new { id = service.Id });
    }

    private async Task Load()
    {
        Categories = await db.Categories.OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }
}
